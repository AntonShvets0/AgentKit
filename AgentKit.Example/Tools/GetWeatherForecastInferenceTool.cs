using System.ComponentModel;
using AgentKit.Attributes;
using AgentKit.Interfaces;

namespace AgentKit.Example.Tools;

public class GetWeatherForecastInferenceTool
    : IInferenceTool
{
    public string Description { get; set; } = "This tool will allow you to get a weather forecast";

    [Tool]
    public object Execute(
        
        [Description("City")]
        string city,
        [Description("You can specify the country for greater accuracy")]
        string? country, // nullable типы необязательны!
        
        [Description("If this parameter is false, then you will get the weather forecast for tomorrow")]
        bool forCurrentDate = true // типы с значением по умолчанию тоже необязательны
        )
    {
        return new
        {
            Temperature = 27
        };
    }
}