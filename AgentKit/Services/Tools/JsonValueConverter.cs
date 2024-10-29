using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using AgentKit.Exceptions;

namespace AgentKit.Services.Tools;

public class JsonValueConverter
{
    public virtual object? ConvertJsonValue(JsonNode jsonValue, Type targetType)
    {
        var nullableType = Nullable.GetUnderlyingType(targetType);
        targetType = nullableType ?? targetType;

        try
        {
            if (targetType.IsEnum)
            {
                return Enum.Parse(targetType, jsonValue!.GetValue<string>());
            }

            if (targetType.IsClass && targetType != typeof(string))
            {
                return DeserializeComplexObject(jsonValue, targetType);
            }
            
            return jsonValue.Deserialize(targetType, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });
        }
        catch (Exception ex)
        {
            throw new CompilerToolException(null, $"Failed to convert parameter to type {targetType.Name}: {ex.Message}");
        }
    }
    
    private object? DeserializeComplexObject(JsonNode jsonValue, Type targetType)
    {
        var instance = Activator.CreateInstance(targetType);
        var jsonObject = jsonValue.AsObject();

        foreach (var prop in targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (jsonObject.TryGetPropertyValue(prop.Name, out var value) && value != null)
            {
                var convertedValue = ConvertJsonValue(value, prop.PropertyType);
                prop.SetValue(instance, convertedValue);
            }
        }

        return instance;
    }
}