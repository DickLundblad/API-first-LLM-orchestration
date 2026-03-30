namespace ApiFirst.LlmOrchestration.Planning;

public sealed class PlanValidationException : Exception
{
    public PlanValidationException(string message) : base(message)
    {
    }
}
