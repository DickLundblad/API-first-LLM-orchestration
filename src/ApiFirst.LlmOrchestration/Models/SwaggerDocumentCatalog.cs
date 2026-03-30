namespace ApiFirst.LlmOrchestration.Models;

public sealed class SwaggerDocumentCatalog
{
    private readonly IReadOnlyDictionary<string, SwaggerOperation> _operationsById;

    public SwaggerDocumentCatalog(
        IReadOnlyList<SwaggerOperation> operations,
        string? serverBasePath = null,
        string? title = null,
        string? version = null,
        string? description = null)
    {
        Operations = operations;
        ServerBasePath = NormalizeBasePath(serverBasePath);
        Title = title;
        Version = version;
        Description = description;
        _operationsById = operations.ToDictionary(operation => operation.OperationId, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<SwaggerOperation> Operations { get; }

    public string ServerBasePath { get; }

    public string? Title { get; }

    public string? Version { get; }

    public string? Description { get; }

    public bool TryGetOperation(string operationId, out SwaggerOperation? operation)
    {
        return _operationsById.TryGetValue(operationId, out operation);
    }

    public SwaggerOperation GetRequiredOperation(string operationId)
    {
        if (_operationsById.TryGetValue(operationId, out var operation))
        {
            return operation;
        }

        throw new KeyNotFoundException($"Operation '{operationId}' was not found in the swagger catalog.");
    }

    private static string NormalizeBasePath(string? serverBasePath)
    {
        if (string.IsNullOrWhiteSpace(serverBasePath))
        {
            return string.Empty;
        }

        return serverBasePath.StartsWith('/') ? serverBasePath : "/" + serverBasePath;
    }
}
