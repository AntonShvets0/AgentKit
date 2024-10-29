using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json.Nodes;
using AgentKit.Exceptions;
using AgentKit.Interfaces;
using AgentKit.Models;

namespace AgentKit.Services.Tools;

public class ToolParameterCompilerService
{
    private readonly ParameterTypeResolver _typeResolver;
    private readonly JsonValueConverter _jsonConverter;
    public JsonSchemaGenerator SchemaGenerator { get; }

    public ToolParameterCompilerService(
        JsonValueConverter? jsonConverter = null,
        JsonSchemaGenerator? jsonSchemaGenerator = null
        )
    {
        jsonConverter ??= new JsonValueConverter();
        
        _typeResolver = new ParameterTypeResolver();
        _jsonConverter = jsonConverter;
        
        SchemaGenerator ??= new JsonSchemaGenerator(_typeResolver);
    }

    public ToolParameter CompileParameters(MethodInfo method)
    {
        var parameters = new ToolParameter
        {
            Required = new string[] { },
            Properties = new Dictionary<string, ToolParameter>()
        };

        var requiredParams = new List<string>();
        
        foreach (var param in method.GetParameters())
        {
            if (_typeResolver.ShouldSkipParameter(param))
                continue;

            var description = GetParameterDescription(param);
            
            if (_typeResolver.IsParameterRequired(param))
                requiredParams.Add(param.Name!);

            // Для сложных типов генерируем полную схему
            if (IsComplexType(param.ParameterType))
            {
                parameters.Properties[param.Name!] = SchemaGenerator.GenerateSchema(param.ParameterType);
                parameters.Properties[param.Name!].Description = description;
            }
            // Для простых типов используем базовое описание
            else
            {
                parameters.Properties[param.Name!] = new ToolParameter
                {
                    Type = _typeResolver.GetParameterType(param.ParameterType),
                    Description = description,
                    Enum = _typeResolver.GetEnumValues(param.ParameterType)
                };
            }
        }

        parameters.Required = requiredParams.ToArray();
        return parameters;
    }

    private string GetParameterDescription(ParameterInfo param)
    {
        // Проверяем различные атрибуты для получения описания
        return param.GetCustomAttribute<DescriptionAttribute>()?.Description
            ?? $"Parameter {param.Name}";
    }

    private bool IsComplexType(Type type)
    {
        var nullableType = Nullable.GetUnderlyingType(type);
        type = nullableType ?? type;

        return type.IsClass 
            && type != typeof(string)
            && !type.IsArray
            && !typeof(IEnumerable<>).IsAssignableFrom(type);
    }

    public object?[] PrepareArguments(
        MethodInfo method, 
        JsonObject jsonParams, 
        IInferenceTool tool,
        IInferenceClient inferenceClient,
        IAgent agent)
    {
        var parameters = method.GetParameters();
        var args = new object?[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            args[i] = PrepareParameter(param, jsonParams, tool, inferenceClient, agent);
        }

        return args;
    }

    private object? PrepareParameter(
        ParameterInfo param,
        JsonObject jsonParams,
        IInferenceTool tool,
        IInferenceClient inferenceClient,
        IAgent agent)
    {
        // Обработка special types (IInferenceClient, IAgent)
        if (typeof(IInferenceClient).IsAssignableFrom(param.ParameterType))
        {
            return inferenceClient;
        }
        
        if (typeof(IAgent).IsAssignableFrom(param.ParameterType))
        {
            return agent;
        }

        // Обработка обычных параметров
        if (jsonParams.TryGetPropertyValue(param.Name!, out var jsonValue))
        {
            if (jsonValue == null)
                return GetDefaultValue(param);

            // Для сложных типов используем специальную конвертацию
            if (IsComplexType(param.ParameterType))
            {
                return _jsonConverter.ConvertJsonValue(jsonValue, param.ParameterType);
            }

            // Для коллекций
            if (IsCollectionType(param.ParameterType))
            {
                return ConvertCollection(jsonValue, param.ParameterType);
            }

            // Для простых типов
            return _jsonConverter.ConvertJsonValue(jsonValue, param.ParameterType);
        }

        // Если параметр не найден в JSON
        return GetDefaultValue(param);
    }

    private bool IsCollectionType(Type type)
    {
        return type.IsArray || 
               (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) ||
               (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>));
    }

    private object? ConvertCollection(JsonNode jsonValue, Type collectionType)
    {
        // Если это не массив в JSON, выбрасываем исключение
        if (jsonValue is not JsonArray jsonArray)
            throw new CompilerToolException(null, $"Expected array for type {collectionType.Name}");

        // Определяем тип элементов коллекции
        Type elementType;
        if (collectionType.IsArray)
        {
            elementType = collectionType.GetElementType()!;
        }
        else
        {
            elementType = collectionType.GetGenericArguments()[0];
        }

        // Создаем список нужного типа
        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;

        // Конвертируем каждый элемент
        foreach (var element in jsonArray)
        {
            var convertedElement = _jsonConverter.ConvertJsonValue(element!, elementType);
            list.Add(convertedElement);
        }

        // Если нужен массив, конвертируем список в массив
        if (collectionType.IsArray)
        {
            var array = Array.CreateInstance(elementType, list.Count);
            list.CopyTo(array, 0);
            return array;
        }

        return list;
    }

    private object? GetDefaultValue(ParameterInfo param)
    {
        if (param.HasDefaultValue)
            return param.DefaultValue;

        if (!_typeResolver.IsParameterRequired(param))
            return null;

        throw new CompilerToolException(null, $"Required parameter {param.Name} was not provided");
    }
}