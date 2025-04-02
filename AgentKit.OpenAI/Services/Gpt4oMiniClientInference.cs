using System.ClientModel;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using AgentKit.Abstractions;
using AgentKit.Interfaces;
using AgentKit.Models;
using AgentKit.Models.Chat;
using AgentKit.Models.Chat.MessageAttachments;
using AgentKit.OpenAI.Models;
using AgentKit.Services.Tools;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using ChatMessage = OpenAI.Chat.ChatMessage;

namespace AgentKit.OpenAI.Services;

public class Gpt4oMiniClientInference(
    ToolCompilerService toolCompilerService,
    IOptions<OpenAIConfiguration> configuration)
    : OpenAIClientInference(toolCompilerService, configuration.Value)
{
    public override string Model { get; protected set; } = "gpt-4o-mini";
}