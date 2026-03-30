using ApiFirst.LlmOrchestration.Models;

namespace ApiFirst.LlmOrchestration.Cli;

public static class SwaggerCatalogPrinter
{
    public static string Print(SwaggerDocumentCatalog catalog)
    {
        var lines = new List<string>
        {
            $"Title: {catalog.Title ?? "Unknown"}",
            $"Version: {catalog.Version ?? "Unknown"}",
            $"Base path: {catalog.ServerBasePath}",
            string.Empty,
            "Operations:"
        };

        foreach (var operation in catalog.Operations.OrderBy(operation => operation.OperationId, StringComparer.OrdinalIgnoreCase))
        {
            lines.Add($"- {operation.OperationId}: {operation.Method} {catalog.ServerBasePath}{operation.Path}");

            if (!string.IsNullOrWhiteSpace(operation.Summary))
            {
                lines.Add($"  {operation.Summary}");
            }

            if (operation.Tags.Count > 0)
            {
                lines.Add($"  Tags: {string.Join(", ", operation.Tags)}");
            }
        }

        return string.Join(Environment.NewLine, lines);
    }
}
