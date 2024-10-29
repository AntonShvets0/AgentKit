using AgentKit.DI.Extensions;
using AgentKit.Example.Agents;
using AgentKit.Models;
using AgentKit.OpenAI.Models;
using AgentKit.OpenAI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInference<Gpt4oClientInference>(InferenceType.Big);
builder.Services.AddInference<Gpt4oMiniClientInference>(InferenceType.Small);

builder.Services.Configure<OpenAIConfiguration>(builder.Configuration.GetSection("OpenAI"));
builder.Services.AddAgentKit();
builder.Services.AddAgent<ModeratorAgent>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();