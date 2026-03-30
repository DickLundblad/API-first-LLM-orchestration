using ApiFirst.LlmOrchestration.Abstractions;
using ApiFirst.LlmOrchestration.Models;

namespace ApiFirst.LlmOrchestration.Orchestration;

public sealed class ApiAgentOrchestrator
{
    private readonly ISwaggerDocumentLoader _documentLoader;
    private readonly IUseCasePlanner _planner;
    private readonly IApiExecutor _executor;

    public ApiAgentOrchestrator(
        ISwaggerDocumentLoader documentLoader,
        IUseCasePlanner planner,
        IApiExecutor executor)
    {
        _documentLoader = documentLoader;
        _planner = planner;
        _executor = executor;
    }

    public async Task<UseCaseExecutionResult> ExecuteAsync(
        string swaggerJsonPath,
        UseCaseRequest request,
        CancellationToken cancellationToken = default)
    {
        var catalog = await _documentLoader.LoadFromFileAsync(swaggerJsonPath, cancellationToken).ConfigureAwait(false);
        return await ExecuteAsync(catalog, request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<UseCaseExecutionResult> ExecuteAsync(
        SwaggerDocumentCatalog catalog,
        UseCaseRequest request,
        CancellationToken cancellationToken = default)
    {
        var plan = await _planner.CreatePlanAsync(request, catalog, cancellationToken).ConfigureAwait(false);

        var results = new List<PlannedActionResult>(plan.Actions.Count);

        foreach (var action in plan.Actions)
        {
            if (!catalog.TryGetOperation(action.OperationId, out var operation) || operation is null)
            {
                throw new InvalidOperationException($"The planner referenced unknown operation '{action.OperationId}'.");
            }

            var result = await _executor.ExecuteAsync(operation, action, cancellationToken).ConfigureAwait(false);
            results.Add(result);
        }

        return new UseCaseExecutionResult(plan, results);
    }
}
