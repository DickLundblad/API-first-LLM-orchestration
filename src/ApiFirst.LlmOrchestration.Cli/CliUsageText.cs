namespace ApiFirst.LlmOrchestration.Cli;

public static class CliUsageText
{
    public static string Help => """
ApiFirst LLM Orchestration

Usage:
  dotnet run --project src/ApiFirst.LlmOrchestration.Cli -- --user-id alice --swagger-url http://localhost:5000/api/swagger.json --goal "Inspect team member 42"
  dotnet run --project src/ApiFirst.LlmOrchestration.Cli -- --user-id alice --swagger-file .\swagger.json --goal "Inspect team member 42"

Options:
  --user-id          User identity used to resolve per-user model settings.
  --swagger-url      URL to a swagger.json document.
  --swagger-file     Local path to a swagger.json document.
  --goal             Plain-language task for the planner.
  --api-base-url     Base URL for the target REST API. Defaults to the origin of --swagger-url.
  --help, -h, /?     Show this help.
""";
}
