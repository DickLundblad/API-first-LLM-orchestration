using Microsoft.Playwright;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Text.Json;

namespace ScreenshotCapture;

/// <summary>
/// Automated screenshot capture tool for InternalAI GUI.
/// Captures screenshots of all mapped GUI features and generates thumbnails.
/// </summary>
class Program
{
    private const string GuiBaseUrl = "http://localhost:3000";
    private const int FullWidth = 1920;
    private const int FullHeight = 1080;
    private const int ThumbWidth = 300;
    private const int ThumbHeight = 200;

    // Default credentials - can be overridden via environment variables
    private static readonly string Username = Environment.GetEnvironmentVariable("INTERNALAI_USERNAME") ?? "admin";
    private static readonly string Password = Environment.GetEnvironmentVariable("INTERNALAI_PASSWORD") ?? "Admin1234!";

    static async Task<int> Main(string[] args)
    {
        var repoRoot = FindRepositoryRoot();
        if (repoRoot is null)
        {
            Console.Error.WriteLine("Error: Could not find repository root. Run this tool from within the repository.");
            return 1;
        }

        var mappingsPath = Path.Combine(repoRoot, "src", "ApiFirst.LlmOrchestration.McpServer", "GuiSupportMappings.json");
        var screenshotsDir = Path.Combine(repoRoot, "docs", "screenshots");

        if (!File.Exists(mappingsPath))
        {
            Console.Error.WriteLine($"Error: GuiSupportMappings.json not found at {mappingsPath}");
            return 1;
        }

        Console.WriteLine("InternalAI GUI Screenshot Capture Tool");
        Console.WriteLine("======================================");
        Console.WriteLine();
        Console.WriteLine($"Repository root: {repoRoot}");
        Console.WriteLine($"Screenshots directory: {screenshotsDir}");
        Console.WriteLine($"GUI URL: {GuiBaseUrl}");
        Console.WriteLine($"Username: {Username}");
        Console.WriteLine();

        // Load mappings
        var mappings = LoadMappings(mappingsPath);
        if (mappings.Count == 0)
        {
            Console.Error.WriteLine("Error: No GUI mappings found.");
            return 1;
        }

        Console.WriteLine($"Found {mappings.Count} GUI features to capture.");
        Console.WriteLine();

        // Check if InternalAI is running
        Console.WriteLine("Checking if InternalAI frontend is running...");
        if (!await IsGuiRunning())
        {
            Console.Error.WriteLine($"Error: InternalAI frontend is not running at {GuiBaseUrl}");
            Console.Error.WriteLine("Please start the frontend with: cd C:\\git\\InternalAI\\frontend && npm start");
            return 1;
        }
        Console.WriteLine("✓ InternalAI frontend is running");
        Console.WriteLine();

        // Install Playwright browsers if needed
        Console.WriteLine("Installing Playwright browsers (if needed)...");
        Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
        Console.WriteLine();

        // Capture screenshots
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = true
        });

        var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = FullWidth, Height = FullHeight }
        });

        var page = await context.NewPageAsync();

        // Navigate to home page first
        Console.WriteLine("Navigating to home page...");
        await page.GotoAsync(GuiBaseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });

        // Handle cookie consent if present - try multiple selectors
        Console.WriteLine("Handling cookie consent...");
        try
        {
            // Try different common cookie accept button selectors
            var cookieSelectors = new[]
            {
                "button:has-text('Accept')",
                "button:has-text('Accept All')",
                "button:has-text('I Accept')",
                "button[data-testid='accept-cookies']",
                "button[data-testid='cookie-accept']",
                ".coi-banner__accept",
                "button.accept-cookies"
            };

            foreach (var selector in cookieSelectors)
            {
                try
                {
                    var button = page.Locator(selector).First;
                    await button.WaitForAsync(new() { Timeout = 2000, State = WaitForSelectorState.Visible });
                    await button.ClickAsync(new() { Force = true }); // Force click to bypass overlay issues
                    Console.WriteLine($"✓ Cookie consent accepted using selector: {selector}");
                    await page.WaitForTimeoutAsync(1000);
                    break;
                }
                catch
                {
                    // Try next selector
                    continue;
                }
            }
        }
        catch
        {
            Console.WriteLine("  (No cookie banner found or already accepted)");
        }

        // Perform login for authenticated pages
        Console.WriteLine();
        Console.WriteLine("Logging in...");
        try
        {
            await page.GotoAsync(GuiBaseUrl + "/login", new() { WaitUntil = WaitUntilState.NetworkIdle });

            // Fill in credentials
            await page.FillAsync("input[name='username'], input[type='text'], input[placeholder*='username' i]", Username);
            await page.FillAsync("input[name='password'], input[type='password'], input[placeholder*='password' i]", Password);

            // Click login button
            await page.ClickAsync("button[type='submit'], button:has-text('Login'), button:has-text('Sign in')");

            // Wait for navigation after login
            await page.WaitForURLAsync(url => !url.Contains("/login"), new() { Timeout = 10000 });

            Console.WriteLine("✓ Successfully logged in");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Login attempt failed: {ex.Message}");
            Console.WriteLine("  Continuing with screenshot capture (some pages may not be accessible)");
        }

        Console.WriteLine();
        Console.WriteLine("Starting screenshot capture...");
        Console.WriteLine();

        var capturedRoutes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var capturedCount = 0;

        foreach (var mapping in mappings)
        {
            // Skip if we already captured this route
            if (capturedRoutes.Contains(mapping.Route))
            {
                Console.WriteLine($"Skipping {mapping.Feature} - already captured route {mapping.Route}");
                continue;
            }

            Console.WriteLine($"Capturing: {mapping.Feature}");
            Console.WriteLine($"  Route: {mapping.Route}");
            Console.WriteLine($"  Files: {mapping.ScreenshotFile}, {mapping.ThumbnailFile}");

            try
            {
                // Navigate to the page
                var url = GuiBaseUrl + mapping.Route;
                await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });

                // Wait a bit for any animations and content to load
                await page.WaitForTimeoutAsync(2000);

                // Capture full screenshot
                var fullPath = Path.Combine(screenshotsDir, mapping.ScreenshotFile);
                await page.ScreenshotAsync(new() { Path = fullPath, FullPage = false });
                Console.WriteLine($"  ✓ Saved: {mapping.ScreenshotFile}");

                // Generate thumbnail
                var thumbPath = Path.Combine(screenshotsDir, mapping.ThumbnailFile);
                GenerateThumbnail(fullPath, thumbPath);
                Console.WriteLine($"  ✓ Saved: {mapping.ThumbnailFile}");

                capturedRoutes.Add(mapping.Route);
                capturedCount++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  ✗ Error: {ex.Message}");
            }

            Console.WriteLine();
        }

        Console.WriteLine("======================================");
        Console.WriteLine($"Capture complete: {capturedCount} unique routes captured");
        Console.WriteLine($"Screenshots saved to: {screenshotsDir}");
        Console.WriteLine();
        Console.WriteLine("Next steps:");
        Console.WriteLine("1. Review the screenshots in docs/screenshots/");
        Console.WriteLine("2. Commit and push: git add docs/screenshots/*.png && git commit -m \"Add GUI screenshots\" && git push");
        Console.WriteLine();

        return 0;
    }

    private static List<GuiMapping> LoadMappings(string mappingsPath)
    {
        var result = new List<GuiMapping>();
        var json = File.ReadAllText(mappingsPath);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("mappings", out var mappingsArray))
        {
            return result;
        }

        foreach (var item in mappingsArray.EnumerateArray())
        {
            var operationId = item.GetProperty("operationId").GetString() ?? "";
            var route = item.GetProperty("guiRoute").GetString() ?? "";
            var feature = item.GetProperty("guiFeature").GetString() ?? "";
            var screenshotUrl = item.TryGetProperty("screenshotUrl", out var ss) ? ss.GetString() : null;
            var thumbnailUrl = item.TryGetProperty("thumbnailUrl", out var th) ? th.GetString() : null;

            if (string.IsNullOrWhiteSpace(screenshotUrl) || string.IsNullOrWhiteSpace(thumbnailUrl))
            {
                continue;
            }

            // Replace parameter placeholders with sample values
            var actualRoute = route
                .Replace("{id}", "1", StringComparison.OrdinalIgnoreCase)
                .Replace("{memberId}", "1", StringComparison.OrdinalIgnoreCase)
                .Replace("{consultantId}", "1", StringComparison.OrdinalIgnoreCase);

            result.Add(new GuiMapping(operationId, actualRoute, feature, screenshotUrl, thumbnailUrl));
        }

        return result;
    }

    private static void GenerateThumbnail(string sourcePath, string thumbPath)
    {
        using var image = Image.Load(sourcePath);
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(ThumbWidth, ThumbHeight),
            Mode = ResizeMode.Crop,
            Position = AnchorPositionMode.Center
        }));
        image.SaveAsPng(thumbPath);
    }

    private static async Task<bool> IsGuiRunning()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await client.GetAsync(GuiBaseUrl);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static string? FindRepositoryRoot()
    {
        var current = Directory.GetCurrentDirectory();
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current, ".git")))
            {
                return current;
            }
            current = Directory.GetParent(current)?.FullName;
        }
        return null;
    }

    private record GuiMapping(
        string OperationId,
        string Route,
        string Feature,
        string ScreenshotFile,
        string ThumbnailFile);
}
