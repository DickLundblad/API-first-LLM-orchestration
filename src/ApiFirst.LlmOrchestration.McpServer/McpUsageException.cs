namespace ApiFirst.LlmOrchestration.McpServer;

public sealed class McpUsageException : Exception
{
    public McpUsageException(string message)
        : base(message)
    {
    }
}
