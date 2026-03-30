namespace ApiFirst.LlmOrchestration.Models;

public sealed record SwaggerOperation(
    string Path,
    string Method,
    string OperationId,
    string? Summary,
    IReadOnlyList<SwaggerParameter> Parameters,
    IReadOnlyList<string> Tags,
    bool HasRequestBody,
    IReadOnlyList<string> SecurityRequirements,
    IReadOnlyList<int> ResponseStatusCodes);
