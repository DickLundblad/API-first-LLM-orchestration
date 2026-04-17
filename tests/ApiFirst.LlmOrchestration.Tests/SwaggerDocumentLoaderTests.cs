using ApiFirst.LlmOrchestration.Swagger;
using NUnit.Framework;

namespace ApiFirst.LlmOrchestration.Tests;

public sealed class SwaggerDocumentLoaderTests
{
    [Test]
    public async Task LoadFromFileAsync_parses_operations_and_parameters()
    {
        var swaggerPath = await TestSwaggerFile.CreateAsync("""
            {
              "openapi": "3.0.1",
              "paths": {
                "/orders/{orderId}": {
                  "get": {
                    "operationId": "GetOrder",
                    "summary": "Gets an order",
                    "parameters": [
                      { "name": "orderId", "in": "path", "required": true, "schema": { "type": "string" } },
                      { "name": "includeHistory", "in": "query", "required": false, "schema": { "type": "boolean" } }
                    ]
                  }
                }
              }
            }
            """);

        var loader = new SwaggerDocumentLoader();

        var catalog = await loader.LoadFromFileAsync(swaggerPath);

        Assert.That(catalog.Operations, Has.Count.EqualTo(1));

        var operation = catalog.GetRequiredOperation("GetOrder");

        Assert.That(operation.Path, Is.EqualTo("/orders/{orderId}"));
        Assert.That(operation.Method, Is.EqualTo("GET"));
        Assert.That(operation.Summary, Is.EqualTo("Gets an order"));
        Assert.That(operation.Parameters, Has.Count.EqualTo(2));
        Assert.That(operation.Parameters[0].Name, Is.EqualTo("orderId"));
        Assert.That(operation.Parameters[0].Location, Is.EqualTo("path"));
    }

    [Test]
    public async Task LoadFromFileAsync_extracts_path_from_absolute_server_url()
    {
        var swaggerPath = await TestSwaggerFile.CreateAsync("""
            {
              "openapi": "3.0.1",
              "servers": [
                { "url": "http://localhost:5000/api" }
              ],
              "paths": {
                "/auth/login": {
                  "post": {
                    "operationId": "Login",
                    "summary": "User login"
                  }
                }
              }
            }
            """);

        var loader = new SwaggerDocumentLoader();

        var catalog = await loader.LoadFromFileAsync(swaggerPath);

        Assert.That(catalog.ServerBasePath, Is.EqualTo("/api"));
    }

    [Test]
    public async Task LoadFromFileAsync_extracts_path_from_absolute_server_url_without_path()
    {
        var swaggerPath = await TestSwaggerFile.CreateAsync("""
            {
              "openapi": "3.0.1",
              "servers": [
                { "url": "http://localhost:5000" }
              ],
              "paths": {
                "/auth/login": {
                  "post": {
                    "operationId": "Login",
                    "summary": "User login"
                  }
                }
              }
            }
            """);

        var loader = new SwaggerDocumentLoader();

        var catalog = await loader.LoadFromFileAsync(swaggerPath);

        Assert.That(catalog.ServerBasePath, Is.EqualTo("/"));
    }

    [Test]
    public async Task LoadFromFileAsync_handles_relative_server_path()
    {
        var swaggerPath = await TestSwaggerFile.CreateAsync("""
            {
              "openapi": "3.0.1",
              "servers": [
                { "url": "/api/v1" }
              ],
              "paths": {
                "/users": {
                  "get": {
                    "operationId": "GetUsers",
                    "summary": "List users"
                  }
                }
              }
            }
            """);

        var loader = new SwaggerDocumentLoader();

        var catalog = await loader.LoadFromFileAsync(swaggerPath);

        Assert.That(catalog.ServerBasePath, Is.EqualTo("/api/v1"));
    }
}

