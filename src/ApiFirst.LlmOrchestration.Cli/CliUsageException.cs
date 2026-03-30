namespace ApiFirst.LlmOrchestration.Cli;

public sealed class CliUsageException : Exception
{
    public CliUsageException(string message) : base(message)
    {
    }
}
