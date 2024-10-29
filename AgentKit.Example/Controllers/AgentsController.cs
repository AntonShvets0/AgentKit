using AgentKit.DI.Factories;
using AgentKit.Example.Agents;
using AgentKit.Example.Models.Moderator;
using Microsoft.AspNetCore.Mvc;

namespace AgentKit.Example.Controllers;

[ApiController]
[Route("[controller]")]
public class AgentsController : ControllerBase
{
    private AgentFactory _agentFactory;

    public AgentsController(AgentFactory agentFactory)
    {
        _agentFactory = agentFactory;
    }
    
    [HttpGet]
    [Route("[action]")]
    public async Task<IActionResult> TestModeratorAgentAsync(string message)
    {
        var agent = _agentFactory.RentAgent<ModeratorAgent>();
        return Ok(await agent.SendRequestAsync(new ModeratorRequest(message)));
    }
}