using ApiFirst.LlmOrchestration.Models;
using ApiFirst.LlmOrchestration.Planning;
using NUnit.Framework;

namespace ApiFirst.LlmOrchestration.Tests;

public sealed class UseCasePlanPromptBuilderTests
{
    [Test]
    public void Build_includes_the_user_goal_operations_and_schema()
    {
        var catalog = new SwaggerDocumentCatalog(
            new[]
            {
                new SwaggerOperation(
                    "/team/{id}",
                    "GET",
                    "GetTeamMember",
                    "Get a team member",
                    new[] { new SwaggerParameter("id", "path", true, "integer") },
                    new[] { "Team" },
                    false,
                    new[] { "SessionAuth" },
                    new[] { 200 })
            },
            "/api",
            "InternalAI API",
            "1.0.0",
            "Example API");

        var builder = new UseCasePlanPromptBuilder();
        var prompt = builder.Build(new UseCaseRequest("Find one team member"), catalog);

        Assert.That(prompt, Does.Contain("You are a planning assistant for an API agent."));
        Assert.That(prompt, Does.Contain("Find one team member"));
        Assert.That(prompt, Does.Contain("GetTeamMember"));
        Assert.That(prompt, Does.Contain("/api/team/{id}"));
        Assert.That(prompt, Does.Contain("Return JSON matching this shape:"));
        Assert.That(prompt, Does.Contain("\"operationId\""));
    }
}
