namespace ApiFirst.LlmOrchestration.Abstractions;

public interface ITextGenerationClient
{
    Task<string> GenerateTextAsync(string prompt, CancellationToken cancellationToken = default);
}
