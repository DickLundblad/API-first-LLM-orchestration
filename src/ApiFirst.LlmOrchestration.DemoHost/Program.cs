using ApiFirst.LlmOrchestration.Abstractions;
using ApiFirst.LlmOrchestration.Configuration;
using ApiFirst.LlmOrchestration.Cli;
using ApiFirst.LlmOrchestration.DemoHost;
using ApiFirst.LlmOrchestration.Models;
using ApiFirst.LlmOrchestration.Orchestration;
using ApiFirst.LlmOrchestration.Planning;
using ApiFirst.LlmOrchestration.Providers;
using ApiFirst.LlmOrchestration.Swagger;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/openapi.json", (HttpRequest request) =>
{
    var baseUrl = $"{request.Scheme}://{request.Host}";
    return Results.Text(DemoHostOpenApiDocument.Build(baseUrl), "application/json");
});

app.MapGet("/operations", async (
    string? swaggerUrl,
    string? swaggerFile,
    ISwaggerDocumentLoader loader,
    CancellationToken cancellationToken) =>
{
    var catalog = await LoadCatalogAsync(loader, swaggerUrl, swaggerFile, cancellationToken).ConfigureAwait(false);
    return Results.Text(PrintCatalog(catalog), "text/plain");
});

app.MapPost("/run", async (
    DemoRunRequest request,
    ISwaggerDocumentLoader loader,
    CancellationToken cancellationToken) =>
{
    var catalog = await LoadCatalogAsync(loader, request.SwaggerUrl, request.SwaggerFile, cancellationToken).ConfigureAwait(false);
    var userId = Require(request.UserId, nameof(request.UserId));
    var goal = Require(request.Goal, nameof(request.Goal));
    var apiBaseUrl = ResolveApiBaseUrl(request.ApiBaseUrl, request.SwaggerUrl, request.SwaggerFile);

    var settingsProvider = new EnvironmentUserModelSettingsProvider();
    var settings = await settingsProvider.GetAsync(userId, cancellationToken).ConfigureAwait(false);
    var textClient = new TextGenerationClientFactory(new HttpClient()).Create(settings);
    var planner = new ValidatingUseCasePlanner(textClient);
    var executor = new HttpApiExecutor(new HttpClient(), apiBaseUrl, catalog.ServerBasePath);
    var orchestrator = new ApiAgentOrchestrator(loader, planner, executor);

    var result = await orchestrator.ExecuteAsync(catalog, new UseCaseRequest(goal), cancellationToken).ConfigureAwait(false);
    return Results.Ok(result);
});

app.Run();

static async Task<SwaggerDocumentCatalog> LoadCatalogAsync(
    ISwaggerDocumentLoader loader,
    string? swaggerUrl,
    string? swaggerFile,
    CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(swaggerUrl) == string.IsNullOrWhiteSpace(swaggerFile))
    {
        throw new InvalidOperationException("Specify exactly one of swaggerUrl or swaggerFile.");
    }

    return swaggerUrl is not null
        ? await loader.LoadFromUrlAsync(swaggerUrl, cancellationToken).ConfigureAwait(false)
        : await loader.LoadFromFileAsync(swaggerFile!, cancellationToken).ConfigureAwait(false);
}

static string Require(string? value, string name)
{
    if (!string.IsNullOrWhiteSpace(value))
    {
        return value;
    }

    throw new InvalidOperationException($"Missing required field '{name}'.");
}

static Uri ResolveApiBaseUrl(string? apiBaseUrl, string? swaggerUrl, string? swaggerFile)
{
    if (!string.IsNullOrWhiteSpace(apiBaseUrl))
    {
        return new Uri(apiBaseUrl, UriKind.Absolute);
    }

    if (!string.IsNullOrWhiteSpace(swaggerUrl))
    {
        var uri = new Uri(swaggerUrl, UriKind.Absolute);
        return new Uri(uri.GetLeftPart(UriPartial.Authority), UriKind.Absolute);
    }

    throw new InvalidOperationException("apiBaseUrl is required when using swaggerFile.");
}

static string PrintCatalog(SwaggerDocumentCatalog catalog)
{
    var lines = new List<string>
    {
        $"Title: {catalog.Title ?? "Unknown"}",
        $"Version: {catalog.Version ?? "Unknown"}",
        $"Base path: {catalog.ServerBasePath}",
        string.Empty,
        "Operations:"
    };

    foreach (var operation in catalog.Operations.OrderBy(operation => operation.OperationId, StringComparer.OrdinalIgnoreCase))
    {
        lines.Add($"- {operation.OperationId}: {operation.Method} {catalog.ServerBasePath}{operation.Path}");

        if (!string.IsNullOrWhiteSpace(operation.Summary))
        {
            lines.Add($"  {operation.Summary}");
        }

        if (operation.Tags.Count > 0)
        {
            lines.Add($"  Tags: {string.Join(", ", operation.Tags)}");
        }
    }

    return string.Join(Environment.NewLine, lines);
}

public sealed record DemoRunRequest(
    string? UserId,
    string? SwaggerUrl,
    string? SwaggerFile,
    string? Goal,
    string? ApiBaseUrl);
