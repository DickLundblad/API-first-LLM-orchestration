using ApiFirst.LlmOrchestration.Abstractions;
using ApiFirst.LlmOrchestration.Models;
using ApiFirst.LlmOrchestration.Orchestration;
using ApiFirst.LlmOrchestration.Swagger;
using NUnit.Framework;

namespace ApiFirst.LlmOrchestration.Tests;

public sealed class ApiAgentOrchestratorTests
{
    [Test]
    public async Task ExecuteAsync_runs_the_planned_actions_in_order()
    {
        var swaggerPath = await TestSwaggerFile.CreateAsync("""
            {
              "openapi": "3.0.1",
              "paths": {
                "/customers/{id}": {
                  "get": {
                    "operationId": "GetCustomer",
                    "summary": "Gets a customer",
                    "parameters": [
                      { "name": "id", "in": "path", "required": true, "schema": { "type": "string" } }
                    ]
                  }
                },
                "/customers": {
                  "post": {
                    "operationId": "CreateCustomer",
                    "summary": "Creates a customer"
                  }
                }
              }
            }
            """);

        var loader = new SwaggerDocumentLoaderStub();
        var planner = new FakePlanner(new UseCasePlan(
            "Create and verify customer",
            "The superuser wants to create a customer and then fetch it.",
            new[]
            {
                new PlannedAction("CreateCustomer", new Dictionary<string, string?>(), """{"name":"Ada"}"""),
                new PlannedAction("GetCustomer", new Dictionary<string, string?> { ["id"] = "42" })
            }));

        var executor = new FakeExecutor();
        var orchestrator = new ApiAgentOrchestrator(loader, planner, executor);

        var result = await orchestrator.ExecuteAsync(swaggerPath, new UseCaseRequest("Create a customer and verify it"));

        Assert.That(result.Plan.Actions, Has.Count.EqualTo(2));
        Assert.That(executor.ExecutedOperations, Is.EqualTo(new[] { "CreateCustomer", "GetCustomer" }));
        Assert.That(result.Results, Has.Count.EqualTo(2));
        Assert.That(result.Results[0].Succeeded, Is.True);
        Assert.That(result.Results[1].Succeeded, Is.True);
    }

    [Test]
    public void ExecuteAsync_throws_when_the_planner_returns_an_unknown_operation()
    {
        var swaggerPath = TestSwaggerFile.CreateSync("""
            {
              "openapi": "3.0.1",
              "paths": {
                "/customers": {
                  "get": {
                    "operationId": "ListCustomers"
                  }
                }
              }
            }
            """);

        var loader = new SwaggerDocumentLoaderStub();
        var planner = new FakePlanner(new UseCasePlan(
            "Invalid plan",
            "This plan references an operation that does not exist in the swagger document.",
            new[] { new PlannedAction("DeleteCustomer", new Dictionary<string, string?>()) }));

        var executor = new FakeExecutor();
        var orchestrator = new ApiAgentOrchestrator(loader, planner, executor);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await orchestrator.ExecuteAsync(swaggerPath, new UseCaseRequest("Delete a customer")));
    }

    private sealed class SwaggerDocumentLoaderStub : ISwaggerDocumentLoader
    {
        public Task<SwaggerDocumentCatalog> LoadFromFileAsync(string swaggerJsonPath, CancellationToken cancellationToken = default)
        {
            return new SwaggerDocumentLoader().LoadFromFileAsync(swaggerJsonPath, cancellationToken);
        }

        public Task<SwaggerDocumentCatalog> LoadFromUrlAsync(string swaggerJsonUrl, CancellationToken cancellationToken = default)
        {
            return new SwaggerDocumentLoader().LoadFromUrlAsync(swaggerJsonUrl, cancellationToken);
        }
    }

    private sealed class FakePlanner : IUseCasePlanner
    {
        private readonly UseCasePlan _plan;

        public FakePlanner(UseCasePlan plan)
        {
            _plan = plan;
        }

        public Task<UseCasePlan> CreatePlanAsync(UseCaseRequest request, SwaggerDocumentCatalog catalog, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_plan);
        }
    }

    private sealed class FakeExecutor : IApiExecutor
    {
        public List<string> ExecutedOperations { get; } = new List<string>();

        public Task<PlannedActionResult> ExecuteAsync(SwaggerOperation operation, PlannedAction action, CancellationToken cancellationToken = default)
        {
            ExecutedOperations.Add(operation.OperationId);
            return Task.FromResult(new PlannedActionResult(operation.OperationId, true, System.Net.HttpStatusCode.OK, "{}"));
        }
    }
}
