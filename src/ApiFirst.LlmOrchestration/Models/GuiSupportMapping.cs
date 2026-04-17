namespace ApiFirst.LlmOrchestration.Models;

/// <summary>
/// Represents a mapping between an API operation and its corresponding GUI feature.
/// </summary>
public sealed record GuiSupportMapping(
    string OperationId,
    string GuiRoute,
    string GuiFeature,
    string Description,
    IReadOnlyDictionary<string, string>? PathParameters = null,
    string? Notes = null,
    string? ScreenshotUrl = null,
    string? ThumbnailUrl = null);

/// <summary>
/// Represents the complete GUI support configuration including base URL and all mappings.
/// </summary>
public sealed record GuiSupportConfiguration(
    string GuiBaseUrl,
    IReadOnlyList<GuiSupportMapping> Mappings,
    IReadOnlyList<string>? Notes = null,
    string? ScreenshotBaseUrl = null);
