using ApiFirst.LlmOrchestration.Abstractions;
using ApiFirst.LlmOrchestration.Models;
using ApiFirst.LlmOrchestration.Planning;
using NUnit.Framework;

namespace ApiFirst.LlmOrchestration.Tests;

public sealed class ValidatingUseCasePlannerTests
{
    [Test]
    public async Task CreatePlanAsync_parses_and_validates_the_llm_response()
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
                    Array.Empty<string>(),
                    new[] { 200 })
            },
            "/api",
            "InternalAI API",
            "1.0.0",
            "Example API");

        var client = new FakeTextGenerationClient("""
            {
              "name": "Inspect a team member",
              "rationale": "Need to fetch one member by id.",
              "actions": [
                {
                  "operationId": "GetTeamMember",
                  "arguments": {
                    "id": "42"
                  }
                }
              ]
            }
            """);

        var planner = new ValidatingUseCasePlanner(client);

        var plan = await planner.CreatePlanAsync(new UseCaseRequest("Inspect member 42"), catalog);

        Assert.That(plan.Name, Is.EqualTo("Inspect a team member"));
        Assert.That(plan.Actions, Has.Count.EqualTo(1));
        Assert.That(plan.Actions[0].OperationId, Is.EqualTo("GetTeamMember"));
        Assert.That(plan.Actions[0].Arguments["id"], Is.EqualTo("42"));
    }

    [Test]
    public void CreatePlanAsync_rejects_missing_required_parameters()
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
                    Array.Empty<string>(),
                    new[] { 200 })
            });

        var client = new FakeTextGenerationClient("""
            {
              "name": "Invalid plan",
              "rationale": "Missing required data.",
              "actions": [
                {
                  "operationId": "GetTeamMember",
                  "arguments": { }
                }
              ]
            }
            """);

        var planner = new ValidatingUseCasePlanner(client);

        Assert.ThrowsAsync<PlanValidationException>(async () =>
            await planner.CreatePlanAsync(new UseCaseRequest("Inspect member 42"), catalog));
    }

    private sealed class FakeTextGenerationClient : ITextGenerationClient
    {
        private readonly string _response;

        public FakeTextGenerationClient(string response)
        {
            _response = response;
        }

        public Task<string> GenerateTextAsync(string prompt, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_response);
        }
    }
}
