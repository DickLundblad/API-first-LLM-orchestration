using ApiFirst.LlmOrchestration.Models;

namespace ApiFirst.LlmOrchestration.Abstractions;

public interface ISwaggerDocumentLoader
{
    Task<SwaggerDocumentCatalog> LoadFromFileAsync(string swaggerJsonPath, CancellationToken cancellationToken = default);

    Task<SwaggerDocumentCatalog> LoadFromUrlAsync(string swaggerJsonUrl, CancellationToken cancellationToken = default);
}
