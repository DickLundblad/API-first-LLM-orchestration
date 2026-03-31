namespace ApiFirst.LlmOrchestration.McpServer;

public static class McpUsageText
{
    public static string Help => """
API-first LLM Orchestration MCP Server

Usage:
  dotnet run --project src/ApiFirst.LlmOrchestration.McpServer -- [options]

Options:
  --user-id <id>         Default user id for tool calls.
  --swagger-url <url>    Default swagger.json URL.
  --swagger-file <path>  Default swagger.json file path.
  --api-base-url <url>   Default API base URL for execution.
  --http-prefix <url>    Run a tiny HTTP wrapper on this prefix.
  -h, --help, /?         Show help.

MCP tools:
  health
  search_operations
  list_operations
  run_use_case

The server speaks MCP over stdio, or via the tiny HTTP wrapper when --http-prefix is set.
""";
}
