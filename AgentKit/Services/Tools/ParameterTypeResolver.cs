using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using AgentKit.Interfaces;
using AgentKit.Models;

namespace AgentKit.Services.Tools;

public class ParameterTypeResolver
{
    public bool ShouldSkipParameter(ParameterInfo param)
    {
        return typeof(IInferenceClient).IsAssignableFrom(param.ParameterType) ||
               typeof(IAgent).IsAssignableFrom(param.ParameterType);
    }

    public bool IsParameterRequired(ParameterInfo param)
    {
        if (Nullable.GetUnderlyingType(param.ParameterType) != null)
            return false;
        
        var nullabilityInfo = new NullabilityInfoContext()
            .Create(param);
        
        var isNullable = nullabilityInfo.WriteState is NullabilityState.Nullable ||
                         param.HasDefaultValue;

        return !isNullable;
    }

    public bool IsParameterRequired(PropertyInfo param)
    {
        if (Nullable.GetUnderlyingType(param.PropertyType) != null)
            return false;
        
        var nullabilityInfo = new NullabilityInfoContext()
            .Create(param);
        
        var isNullable = nullabilityInfo.WriteState is NullabilityState.Nullable;

        return !isNullable;
    }

    public string GetParameterType(Type type)
    {
        var nullableType = Nullable.GetUnderlyingType(type);
        type = nullableType ?? type;

        return type switch
        {
            Type t when t == typeof(string) => "string",
            Type t when t == typeof(int) => "number",
            Type t when t == typeof(long) => "number",
            Type t when t == typeof(float) => "number",
            Type t when t == typeof(double) => "number",
            Type t when t == typeof(decimal) => "number",
            Type t when t == typeof(bool) => "boolean",
            Type t when t.IsEnum => "string",
            Type t when t.IsArray => "array",
            Type t when t.IsClass && t != typeof(string) => "object",
            _ => "object"
        };
    }
    
    public string[]? GetEnumValues(Type type)
    {
        var nullableType = Nullable.GetUnderlyingType(type);
        type = nullableType ?? type;

        if (!type.IsEnum) return null;

        return Enum.GetNames(type);
    }
}