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
            Type = "object",
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

            // Если это массив, создаем специальную схему для массива
            if (IsArrayType(param.ParameterType))
            {
                var elementType = param.ParameterType.GetElementType() 
                                 ?? (param.ParameterType.IsGenericType 
                                    ? param.ParameterType.GetGenericArguments()[0] 
                                    : typeof(object));
                
                var itemParameter = IsComplexType(elementType) 
                    ? SchemaGenerator.GenerateSchema(elementType) 
                    : new ToolParameter { Type = _typeResolver.GetParameterType(elementType) };
                
                // Убираем null-поля из схемы элемента
                RemoveNullFields(itemParameter);
                
                parameters.Properties[param.Name!] = new ToolParameter
                {
                    Type = "array",
                    Description = description,
                    Items = itemParameter
                };
            }
            // Для сложных типов генерируем полную схему
            else if (IsComplexType(param.ParameterType))
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
        
        // Чистим схему от null-полей
        RemoveNullFields(parameters);
        
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

    private bool IsArrayType(Type type)
    {
        return type.IsArray || 
               (type.IsGenericType && 
                (type.GetGenericTypeDefinition() == typeof(List<>) || 
                 type.GetGenericTypeDefinition() == typeof(IEnumerable<>) || 
                 type.GetGenericTypeDefinition() == typeof(ICollection<>) || 
                 type.GetGenericTypeDefinition() == typeof(IList<>)));
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

            // Для массивов
            if (IsArrayType(param.ParameterType))
            {
                return ConvertArray(jsonValue, param.ParameterType);
            }
            
            // Для сложных типов используем специальную конвертацию
            if (IsComplexType(param.ParameterType))
            {
                return _jsonConverter.ConvertJsonValue(jsonValue, param.ParameterType);
            }

            // Для простых типов
            return _jsonConverter.ConvertJsonValue(jsonValue, param.ParameterType);
        }

        // Если параметр не найден в JSON
        return GetDefaultValue(param);
    }

    private object? ConvertArray(JsonNode jsonValue, Type arrayType)
    {
        // Если это не массив в JSON, выбрасываем исключение
        if (jsonValue is not JsonArray jsonArray)
            throw new CompilerToolException(null, $"Expected array for type {arrayType.Name}");

        // Определяем тип элементов массива
        Type elementType;
        if (arrayType.IsArray)
        {
            elementType = arrayType.GetElementType()!;
        }
        else if (arrayType.IsGenericType)
        {
            elementType = arrayType.GetGenericArguments()[0];
        }
        else
        {
            throw new CompilerToolException(null, $"Cannot determine element type for {arrayType.Name}");
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
        if (arrayType.IsArray)
        {
            var array = Array.CreateInstance(elementType, list.Count);
            list.CopyTo(array, 0);
            return array;
        }
        
        // Если нужен конкретный тип коллекции
        if (arrayType.IsGenericType)
        {
            // Если нужно вернуть IEnumerable<T>, можно вернуть список, так как он реализует этот интерфейс
            if (arrayType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return list;
            }
            
            // Для ICollection<T>, IList<T> или List<T> возвращаем список, так как он реализует эти интерфейсы
            if (arrayType.GetGenericTypeDefinition() == typeof(List<>) ||
                arrayType.GetGenericTypeDefinition() == typeof(IList<>) ||
                arrayType.GetGenericTypeDefinition() == typeof(ICollection<>))
            {
                return list;
            }
            
            // Для других типов генериков (редкий случай) пытаемся создать экземпляр
            try
            {
                var instance = Activator.CreateInstance(arrayType);
                // Если это коллекция, пытаемся добавить элементы
                if (instance is IList typedList)
                {
                    foreach (var item in list)
                    {
                        typedList.Add(item);
                    }
                }
                return instance;
            }
            catch (Exception ex)
            {
                throw;
            }
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
    
    /// <summary>
    /// Удаляет все поля со значением null из ToolParameter
    /// </summary>
    private void RemoveNullFields(ToolParameter parameter)
    {
        if (parameter == null) return;
        
        // Если тип не задан, устанавливаем object для корневых объектов
        if (parameter.Type == null)
        {
            parameter.Type = "object";
        }
        
        // Удаляем null-поля
        if (parameter.Description == null) parameter.Description = null;
        if (parameter.Enum == null || !parameter.Enum.Any()) parameter.Enum = null;
        if (parameter.Required == null || !parameter.Required.Any()) parameter.Required = null;
        
        // Рекурсивно обрабатываем properties
        if (parameter.Properties != null)
        {
            foreach (var prop in parameter.Properties.Values)
            {
                RemoveNullFields(prop);
            }
        }
        
        // Рекурсивно обрабатываем items
        if (parameter.Items != null)
        {
            RemoveNullFields(parameter.Items);
        }
    }
}