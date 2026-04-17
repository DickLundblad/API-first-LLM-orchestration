using ApiFirst.LlmOrchestration.Cli;
using ApiFirst.LlmOrchestration.Models;
using NUnit.Framework;

namespace ApiFirst.LlmOrchestration.Tests.DemoHost;

public sealed class SwaggerCatalogPrinterTests
{
    [Test]
    public void Print_renders_a_concise_operation_list()
    {
        var catalog = new SwaggerDocumentCatalog(
            new[]
            {
                new SwaggerOperation(
                    "/team",
                    "GET",
                    "GetTeamMembers",
                    "Get team members",
                    Array.Empty<SwaggerParameter>(),
                    new[] { "Team" },
                    false,
                    Array.Empty<string>(),
                    new[] { 200 })
            },
            "/api",
            "InternalAI API",
            "1.0.0",
            "Example API");

        var output = SwaggerCatalogPrinter.Print(catalog);

        Assert.That(output, Does.Contain("Title: InternalAI API"));
        Assert.That(output, Does.Contain("- GetTeamMembers: GET /api/team"));
        Assert.That(output, Does.Contain("Tags: Team"));
    }
}
