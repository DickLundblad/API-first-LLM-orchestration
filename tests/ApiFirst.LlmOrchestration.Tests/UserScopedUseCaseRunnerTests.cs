using ApiFirst.LlmOrchestration.Abstractions;
using ApiFirst.LlmOrchestration.Configuration;
using ApiFirst.LlmOrchestration.Models;
using ApiFirst.LlmOrchestration.Orchestration;
using ApiFirst.LlmOrchestration.Providers;
using ApiFirst.LlmOrchestration.Swagger;
using NUnit.Framework;

namespace ApiFirst.LlmOrchestration.Tests;

public sealed class UserScopedUseCaseRunnerTests
{
    [Test]
    public async Task RunFromUrlAsync_uses_the_users_settings_to_plan_and_execute()
    {
        var swaggerPath = await TestSwaggerFile.CreateAsync("""
            {
              "openapi": "3.0.1",
              "servers": [{ "url": "/api" }],
              "info": { "title": "InternalAI API", "version": "1.0.0" },
              "paths": {
                "/team/{id}": {
                  "get": {
                    "operationId": "GetTeamMember",
                    "summary": "Get a team member",
                    "parameters": [
                      { "name": "id", "in": "path", "required": true, "schema": { "type": "integer" } }
                    ]
                  }
                }
              }
            }
            """);

        var settingsProvider = new InMemoryUserModelSettingsProvider(new[]
        {
            new UserModelSettings("alice", "openai", "gpt-5.4-mini", "key-alice")
        });

        var loader = new SwaggerDocumentLoader();
        var executor = new RecordingExecutor();
        var factory = new StubFactory(new StubTextGenerationClient("""
            {
              "name": "Lookup team member",
              "rationale": "Need to inspect member 42.",
              "actions": [
                {
                  "operationId": "GetTeamMember",
                  "arguments": { "id": "42" }
                }
              ]
            }
            """));

        var runner = new UserScopedUseCaseRunner(settingsProvider, factory, loader, executor);

        var result = await runner.RunFromFileAsync("alice", swaggerPath, new UseCaseRequest("Look up member 42"));

        Assert.That(result.Plan.Name, Is.EqualTo("Lookup team member"));
        Assert.That(executor.ExecutedOperationIds, Is.EqualTo(new[] { "GetTeamMember" }));
        Assert.That(factory.LastSettings!.UserId, Is.EqualTo("alice"));
        Assert.That(factory.LastSettings.Model, Is.EqualTo("gpt-5.4-mini"));
    }

    private sealed class RecordingExecutor : IApiExecutor
    {
        public List<string> ExecutedOperationIds { get; } = new();

        public Task<PlannedActionResult> ExecuteAsync(SwaggerOperation operation, PlannedAction action, CancellationToken cancellationToken = default)
        {
            ExecutedOperationIds.Add(operation.OperationId);
            return Task.FromResult(new PlannedActionResult(operation.OperationId, true, System.Net.HttpStatusCode.OK, "{}"));
        }
    }

    private sealed class StubTextGenerationClient : ITextGenerationClient
    {
        private readonly string _response;

        public StubTextGenerationClient(string response)
        {
            _response = response;
        }

        public Task<string> GenerateTextAsync(string prompt, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_response);
        }
    }

    private sealed class StubFactory : ITextGenerationClientFactory
    {
        private readonly ITextGenerationClient _client;

        public StubFactory(ITextGenerationClient client)
        {
            _client = client;
        }

        public UserModelSettings? LastSettings { get; private set; }

        public ITextGenerationClient Create(UserModelSettings settings)
        {
            LastSettings = settings;
            return _client;
        }
    }
}
