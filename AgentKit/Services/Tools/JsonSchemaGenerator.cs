﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgentKit.Models;

namespace AgentKit.Services.Tools;

public class JsonSchemaGenerator
{
    private ParameterTypeResolver _parameterTypeResolver;

    public JsonSchemaGenerator(ParameterTypeResolver parameterTypeResolver)
    {
        _parameterTypeResolver = parameterTypeResolver;
    }
    
    private ToolParameter GetParameterSchema(Type type, string description)
    {
        // Обработка массивов и коллекций
        if (type.IsArray || (type.IsGenericType && typeof(System.Collections.IEnumerable).IsAssignableFrom(type)))
        {
            Type elementType = type.IsArray 
                ? type.GetElementType() 
                : type.GetGenericArguments()[0];
            
            return new ToolParameter
            {
                Type = "array",
                Description = description,
                Items = GetParameterSchema(elementType, $"Item of {description}")
            };
        }

        var schema = new ToolParameter
        {
            Type = _parameterTypeResolver.GetParameterType(type),
            Description = description
        };

        if (type.IsEnum)
        {
            schema.Enum = Enum.GetNames(type);
            return schema;
        }

        if (type.IsClass && type != typeof(string))
        {
            var properties = new Dictionary<string, ToolParameter>();
            var required = new List<string>();

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var propDescription = prop.GetCustomAttribute<DescriptionAttribute>()?.Description
                                      ?? prop.GetCustomAttribute<DisplayAttribute>()?.Description
                                      ?? $"Property {prop.Name}";

                if (prop.GetCustomAttribute<RequiredAttribute>() != null)
                {
                    required.Add(prop.Name);
                }

                properties[prop.Name] = GetParameterSchema(prop.PropertyType, propDescription);
            }

            schema.Properties = properties;
            schema.Required = required.Count > 0 ? required.ToArray() : null;
        }

        return schema;
    }
    
    public string SerializeJsonSchema(ToolParameter parameter)
    {
        return JsonSerializer.Serialize(parameter, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }
    
    public ToolParameter GenerateSchema(Type type)
    {
        // Обработка массивов на верхнем уровне
        if (type.IsArray || (type.IsGenericType && typeof(System.Collections.IEnumerable).IsAssignableFrom(type)))
        {
            Type elementType = type.IsArray 
                ? type.GetElementType() 
                : type.GetGenericArguments()[0];
            
            return new ToolParameter
            {
                Type = "array",
                Items = GetParameterSchema(elementType, $"Item of {type.Name}")
            };
        }

        var properties = new Dictionary<string, ToolParameter>();
        var required = new List<string>();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // Получаем описание из атрибута
            var description = prop.GetCustomAttribute<DescriptionAttribute>()?.Description
                              ?? prop.GetCustomAttribute<DisplayAttribute>()?.Description
                              ?? $"Property {prop.Name}";

            // Проверяем, является ли свойство обязательным
            if (_parameterTypeResolver.IsParameterRequired(prop))
            {
                required.Add(prop.Name);
            }

            // Рекурсивно генерируем схему для свойства
            properties[prop.Name] = GetParameterSchema(prop.PropertyType, description);
        }

        return new ToolParameter
        {
            Type = "object",
            Properties = properties,
            Required = required.Count > 0 ? required.ToArray() : null
        };
    }
}