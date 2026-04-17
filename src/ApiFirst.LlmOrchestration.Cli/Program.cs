using ApiFirst.LlmOrchestration.Models;
using ApiFirst.LlmOrchestration.Orchestration;
using ApiFirst.LlmOrchestration.Planning;
using ApiFirst.LlmOrchestration.Providers;
using ApiFirst.LlmOrchestration.Swagger;

namespace ApiFirst.LlmOrchestration.Cli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var options = CliOptions.Parse(args);
            if (options.Help)
            {
                Console.WriteLine(CliUsageText.Help);
                return 0;
            }

            var swaggerLoader = new SwaggerDocumentLoader();
            var catalog = options.SwaggerFile is not null
                ? await swaggerLoader.LoadFromFileAsync(options.SwaggerFile).ConfigureAwait(false)
                : await swaggerLoader.LoadFromUrlAsync(options.SwaggerUrl!).ConfigureAwait(false);

            if (options.ListOperations)
            {
                Console.WriteLine(SwaggerCatalogPrinter.Print(catalog));
                return 0;
            }

            var userId = options.UserId ?? throw new CliUsageException("Missing required option '--user-id'.");
            var goal = options.Goal ?? throw new CliUsageException("Missing required option '--goal'.");

            var settingsProvider = new EnvironmentUserModelSettingsProvider();
            var settings = await settingsProvider.GetAsync(userId).ConfigureAwait(false);
            var clientFactory = new TextGenerationClientFactory(new HttpClient());
            var planner = new ValidatingUseCasePlanner(clientFactory.Create(settings));
            var apiBaseUrl = new Uri(options.ApiBaseUrl, UriKind.Absolute);
            var executor = new HttpApiExecutor(new HttpClient(), apiBaseUrl, catalog.ServerBasePath);
            var orchestrator = new ApiAgentOrchestrator(swaggerLoader, planner, executor);

            var result = await orchestrator.ExecuteAsync(catalog, new UseCaseRequest(goal)).ConfigureAwait(false);

            Console.WriteLine($"Plan: {result.Plan.Name}");
            Console.WriteLine(result.Plan.Rationale);
            Console.WriteLine($"Actions executed: {result.Results.Count}");
            return 0;
        }
        catch (CliUsageException ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine();
            Console.Error.WriteLine(CliUsageText.Help);
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
    }
}
