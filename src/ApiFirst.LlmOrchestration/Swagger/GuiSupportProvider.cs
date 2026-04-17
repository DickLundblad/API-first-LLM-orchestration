using ApiFirst.LlmOrchestration.Models;
using System.Text.Json;

namespace ApiFirst.LlmOrchestration.Swagger;

/// <summary>
/// Loads and queries GUI support mappings for API operations.
/// </summary>
public sealed class GuiSupportProvider
{
    private readonly GuiSupportConfiguration? _configuration;
    private readonly Dictionary<string, GuiSupportMapping> _mappingsByOperationId;

    public GuiSupportProvider(string? mappingsFilePath = null)
    {
        _mappingsByOperationId = new Dictionary<string, GuiSupportMapping>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(mappingsFilePath))
        {
            // Try default location
            var defaultPath = Path.Combine(AppContext.BaseDirectory, "GuiSupportMappings.json");
            mappingsFilePath = File.Exists(defaultPath) ? defaultPath : null;
        }

        if (!string.IsNullOrWhiteSpace(mappingsFilePath) && File.Exists(mappingsFilePath))
        {
            try
            {
                var json = File.ReadAllText(mappingsFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };

                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                var guiBaseUrl = root.GetProperty("guiBaseUrl").GetString() ?? string.Empty;
                var screenshotBaseUrl = root.TryGetProperty("screenshotBaseUrl", out var screenshotBaseElement)
                    ? screenshotBaseElement.GetString()
                    : null;

                var mappingsArray = root.GetProperty("mappings");
                var mappings = new List<GuiSupportMapping>();

                foreach (var item in mappingsArray.EnumerateArray())
                {
                    var operationId = item.GetProperty("operationId").GetString() ?? string.Empty;
                    var guiRoute = item.GetProperty("guiRoute").GetString() ?? string.Empty;
                    var guiFeature = item.GetProperty("guiFeature").GetString() ?? string.Empty;
                    var description = item.GetProperty("description").GetString() ?? string.Empty;

                    Dictionary<string, string>? pathParameters = null;
                    if (item.TryGetProperty("pathParameters", out var pathParamsElement))
                    {
                        pathParameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var param in pathParamsElement.EnumerateObject())
                        {
                            pathParameters[param.Name] = param.Value.GetString() ?? string.Empty;
                        }
                    }

                    var mappingNotes = item.TryGetProperty("notes", out var notesElement)
                        ? notesElement.GetString()
                        : null;

                    var screenshotUrl = item.TryGetProperty("screenshotUrl", out var screenshotElement)
                        ? screenshotElement.GetString()
                        : null;

                    var thumbnailUrl = item.TryGetProperty("thumbnailUrl", out var thumbnailElement)
                        ? thumbnailElement.GetString()
                        : null;

                    var mapping = new GuiSupportMapping(
                        operationId,
                        guiRoute,
                        guiFeature,
                        description,
                        pathParameters,
                        mappingNotes,
                        screenshotUrl,
                        thumbnailUrl);

                    mappings.Add(mapping);
                    _mappingsByOperationId[operationId] = mapping;
                }

                List<string>? notes = null;
                if (root.TryGetProperty("notes", out var notesArray))
                {
                    notes = new List<string>();
                    foreach (var note in notesArray.EnumerateArray())
                    {
                        var noteText = note.GetString();
                        if (!string.IsNullOrWhiteSpace(noteText))
                        {
                            notes.Add(noteText);
                        }
                    }
                }

                _configuration = new GuiSupportConfiguration(guiBaseUrl, mappings, notes, screenshotBaseUrl);
            }
            catch (Exception ex)
            {
                // Log warning but don't fail - GUI support is optional
                Console.Error.WriteLine($"Warning: Failed to load GUI support mappings from '{mappingsFilePath}': {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Gets the GUI support configuration if available.
    /// </summary>
    public GuiSupportConfiguration? Configuration => _configuration;

    /// <summary>
    /// Checks if an operation has GUI support.
    /// </summary>
    public bool HasGuiSupport(string operationId)
    {
        return _mappingsByOperationId.ContainsKey(operationId);
    }

    /// <summary>
    /// Gets the GUI support mapping for an operation, or null if not supported.
    /// </summary>
    public GuiSupportMapping? GetGuiSupport(string operationId)
    {
        return _mappingsByOperationId.TryGetValue(operationId, out var mapping) ? mapping : null;
    }

    /// <summary>
    /// Gets all operations that have GUI support.
    /// </summary>
    public IReadOnlyList<GuiSupportMapping> GetAllMappings()
    {
        return _configuration?.Mappings ?? Array.Empty<GuiSupportMapping>();
    }

    /// <summary>
    /// Constructs the full GUI URL for an operation with the provided parameter values.
    /// </summary>
    public string? GetGuiUrl(string operationId, IReadOnlyDictionary<string, string?>? parameterValues = null)
    {
        if (_configuration is null || !_mappingsByOperationId.TryGetValue(operationId, out var mapping))
        {
            return null;
        }

        var route = mapping.GuiRoute;

        // Replace path parameters if provided
        if (mapping.PathParameters is not null && parameterValues is not null)
        {
            foreach (var pathParam in mapping.PathParameters)
            {
                var placeholder = "{" + pathParam.Key + "}";
                if (parameterValues.TryGetValue(pathParam.Value, out var value) && !string.IsNullOrWhiteSpace(value))
                {
                    route = route.Replace(placeholder, value, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        return _configuration.GuiBaseUrl.TrimEnd('/') + route;
    }
}
