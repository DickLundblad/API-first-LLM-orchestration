using ApiFirst.LlmOrchestration.Swagger;
using NUnit.Framework;

namespace ApiFirst.LlmOrchestration.Tests;

public sealed class SwaggerDocumentLoaderRealSpecTests
{
    [Test]
    public async Task LoadFromUrlAsync_parses_the_real_api_base_path_and_metadata()
    {
        var loader = new SwaggerDocumentLoader();

        var catalog = await loader.LoadFromUrlAsync("http://localhost:5000/api/swagger.json");

        Assert.That(catalog.ServerBasePath, Is.EqualTo("/api"));
        Assert.That(catalog.Title, Is.EqualTo("InternalAI API"));
        Assert.That(catalog.Version, Is.EqualTo("1.0.0"));
        Assert.That(catalog.Operations, Is.Not.Empty);

        var securedOperation = catalog.Operations.FirstOrDefault(operation => operation.SecurityRequirements.Contains("SessionAuth"));

        Assert.That(securedOperation, Is.Not.Null);
        Assert.That(securedOperation!.Path, Does.StartWith("/"));
        Assert.That(securedOperation.ResponseStatusCodes, Is.Not.Empty);
        Assert.That(catalog.Operations.Any(operation => operation.Path == "/health" && operation.Method == "GET"), Is.True);
    }
}
