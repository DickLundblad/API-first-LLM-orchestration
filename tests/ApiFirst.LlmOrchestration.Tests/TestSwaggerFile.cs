namespace ApiFirst.LlmOrchestration.Tests;

internal static class TestSwaggerFile
{
    public static async Task<string> CreateAsync(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".swagger.json");
        await File.WriteAllTextAsync(path, json);
        return path;
    }

    public static string CreateSync(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".swagger.json");
        File.WriteAllText(path, json);
        return path;
    }
}
