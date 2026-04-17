using ApiFirst.LlmOrchestration.Abstractions;
using ApiFirst.LlmOrchestration.Models;
using System.Text.RegularExpressions;

namespace ApiFirst.LlmOrchestration.McpServer;

public sealed class DeterministicFallbackPlanner : IUseCasePlanner
{
    public Task<UseCasePlan> CreatePlanAsync(UseCaseRequest request, SwaggerDocumentCatalog catalog, CancellationToken cancellationToken = default)
    {
        var operation = SelectOperation(request.Objective, catalog);
        var arguments = BuildArguments(operation);
        var requestBodyJson = operation.HasRequestBody ? "{}" : null;

        var plan = new UseCasePlan(
            "Fallback plan",
            "Generated without external LLM license using deterministic operation matching.",
            new[]
            {
                new PlannedAction(operation.OperationId, arguments, requestBodyJson)
            });

        return Task.FromResult(plan);
    }

    private static SwaggerOperation SelectOperation(string goal, SwaggerDocumentCatalog catalog)
    {
        var tokens = Tokenize(goal).ToArray();

        var best = catalog.Operations
            .Select(operation => new
            {
                Operation = operation,
                Score = Score(operation, tokens)
            })
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Operation.Method.Equals("GET", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .FirstOrDefault();

        if (best is not null && best.Score > 0)
        {
            return best.Operation;
        }

        return catalog.Operations.FirstOrDefault(operation => operation.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
            ?? catalog.Operations.First();
    }

    private static int Score(SwaggerOperation operation, IReadOnlyCollection<string> tokens)
    {
        if (tokens.Count == 0)
        {
            return 0;
        }

        var haystack = string.Join(' ', new[]
        {
            operation.OperationId,
            operation.Summary,
            operation.Path,
            string.Join(' ', operation.Tags)
        }.Where(value => !string.IsNullOrWhiteSpace(value))).ToLowerInvariant();

        return tokens.Count(token => haystack.Contains(token, StringComparison.Ordinal));
    }

    private static IReadOnlyDictionary<string, string?> BuildArguments(SwaggerOperation operation)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var parameter in operation.Parameters.Where(parameter => parameter.Required))
        {
            result[parameter.Name] = DefaultValue(parameter.Name);
        }

        return result;
    }

    private static string DefaultValue(string parameterName)
    {
        var name = parameterName.ToLowerInvariant();

        if (name is "page")
        {
            return "1";
        }

        if (name is "page_size" or "pagesize")
        {
            return "50";
        }

        if (name.Contains("id", StringComparison.Ordinal))
        {
            return "1";
        }

        if (name.StartsWith("is", StringComparison.Ordinal) || name.StartsWith("has", StringComparison.Ordinal) || name.Contains("count", StringComparison.Ordinal))
        {
            return "false";
        }

        return "all";
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        return Regex.Matches(text ?? string.Empty, "[A-Za-z0-9_]+")
            .Select(match => match.Value.ToLowerInvariant())
            .Where(token => token.Length >= 3)
            .Distinct();
    }
}
