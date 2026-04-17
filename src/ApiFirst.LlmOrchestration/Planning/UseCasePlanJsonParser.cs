using ApiFirst.LlmOrchestration.Models;
using System.Text.Json;

namespace ApiFirst.LlmOrchestration.Planning;

public sealed class UseCasePlanJsonParser
{
    public UseCasePlan Parse(string json, SwaggerDocumentCatalog catalog)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var name = ReadRequiredString(root, "name");
        var rationale = ReadRequiredString(root, "rationale");

        if (!root.TryGetProperty("actions", out var actionsElement) || actionsElement.ValueKind != JsonValueKind.Array)
        {
            throw new PlanValidationException("The plan must contain an 'actions' array.");
        }

        var actions = new List<PlannedAction>();
        foreach (var actionElement in actionsElement.EnumerateArray())
        {
            actions.Add(ParseAction(actionElement, catalog));
        }

        if (actions.Count == 0)
        {
            throw new PlanValidationException("The plan must contain at least one action.");
        }

        return new UseCasePlan(name, rationale, actions);
    }

    private static PlannedAction ParseAction(JsonElement actionElement, SwaggerDocumentCatalog catalog)
    {
        var operationId = ReadRequiredString(actionElement, "operationId");
        var operation = catalog.GetRequiredOperation(operationId);

        if (!actionElement.TryGetProperty("arguments", out var argumentsElement) || argumentsElement.ValueKind != JsonValueKind.Object)
        {
            throw new PlanValidationException($"Action '{operationId}' must contain an 'arguments' object.");
        }

        var arguments = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var argumentProperty in argumentsElement.EnumerateObject())
        {
            arguments[argumentProperty.Name] = argumentProperty.Value.ValueKind == JsonValueKind.Null ? null : argumentProperty.Value.GetString();
        }

        foreach (var requiredParameter in operation.Parameters.Where(parameter => parameter.Required))
        {
            if (!arguments.TryGetValue(requiredParameter.Name, out var value) || string.IsNullOrWhiteSpace(value))
            {
                throw new PlanValidationException($"Action '{operationId}' is missing required parameter '{requiredParameter.Name}'.");
            }
        }

        string? requestBodyJson = null;
        if (actionElement.TryGetProperty("requestBodyJson", out var requestBodyElement))
        {
            requestBodyJson = requestBodyElement.ValueKind == JsonValueKind.Null ? null : requestBodyElement.GetString();
        }

        if (operation.HasRequestBody && string.IsNullOrWhiteSpace(requestBodyJson))
        {
            throw new PlanValidationException($"Action '{operationId}' must include 'requestBodyJson' because the operation expects a request body.");
        }

        return new PlannedAction(operationId, arguments, requestBodyJson);
    }

    private static string ReadRequiredString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var valueElement))
        {
            throw new PlanValidationException($"The plan is missing '{propertyName}'.");
        }

        var value = valueElement.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new PlanValidationException($"The plan contains an empty '{propertyName}'.");
        }

        return value;
    }
}
