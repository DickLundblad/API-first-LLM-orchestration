using ApiFirst.LlmOrchestration.Models;

namespace ApiFirst.LlmOrchestration.Abstractions;

public interface IUseCasePlanner
{
    Task<UseCasePlan> CreatePlanAsync(
        UseCaseRequest request,
        SwaggerDocumentCatalog catalog,
        CancellationToken cancellationToken = default);
}
