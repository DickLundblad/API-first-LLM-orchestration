using ApiFirst.LlmOrchestration.Abstractions;
using ApiFirst.LlmOrchestration.Models;

namespace ApiFirst.LlmOrchestration.McpServer;

public sealed class StaticUseCasePlanner : IUseCasePlanner
{
    private readonly UseCasePlan _plan;

    public StaticUseCasePlanner(UseCasePlan plan)
    {
        _plan = plan;
    }

    public Task<UseCasePlan> CreatePlanAsync(UseCaseRequest request, SwaggerDocumentCatalog catalog, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_plan);
    }
}
