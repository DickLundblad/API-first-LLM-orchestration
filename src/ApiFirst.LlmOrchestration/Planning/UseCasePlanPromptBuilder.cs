using System.Text;
using ApiFirst.LlmOrchestration.Models;

namespace ApiFirst.LlmOrchestration.Planning;

public sealed class UseCasePlanPromptBuilder
{
    public string Build(UseCaseRequest request, SwaggerDocumentCatalog catalog)
    {
        var builder = new StringBuilder();

        builder.AppendLine("You are a planning assistant for an API agent.");
        builder.AppendLine("Create a safe, minimal, ordered use-case plan as JSON only.");
        builder.AppendLine();
        builder.AppendLine("User goal:");
        builder.AppendLine(request.Objective);

        if (!string.IsNullOrWhiteSpace(request.Context))
        {
            builder.AppendLine();
            builder.AppendLine("Context:");
            builder.AppendLine(request.Context);
        }

        builder.AppendLine();
        builder.AppendLine("API:");
        builder.AppendLine($"Title: {catalog.Title ?? "Unknown"}");
        builder.AppendLine($"Version: {catalog.Version ?? "Unknown"}");
        builder.AppendLine($"Base path: {catalog.ServerBasePath}");
        builder.AppendLine();
        builder.AppendLine("Available operations:");

        foreach (var operation in catalog.Operations.OrderBy(operation => operation.OperationId, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"- {operation.OperationId}: {operation.Method} {catalog.ServerBasePath}{operation.Path} [{string.Join(", ", operation.Tags)}]");

            if (!string.IsNullOrWhiteSpace(operation.Summary))
            {
                builder.AppendLine($"  Summary: {operation.Summary}");
            }

            if (operation.Parameters.Count > 0)
            {
                var parameters = string.Join(", ", operation.Parameters.Select(parameter =>
                    $"{parameter.Name}({parameter.Location}{(parameter.Required ? ", required" : string.Empty)})"));
                builder.AppendLine($"  Parameters: {parameters}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("Return JSON matching this shape:");
        builder.AppendLine(PlannerJsonSchema.Text);

        return builder.ToString();
    }
}

