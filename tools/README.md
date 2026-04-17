# Automated Screenshot Capture

This directory contains tools for automatically capturing screenshots of InternalAI GUI features.

## Overview

The screenshot capture tool automates the process of:
1. Reading GUI feature mappings from `GuiSupportMappings.json`
2. Launching a headless Chrome browser
3. Navigating to each GUI route
4. Capturing full-resolution screenshots (1920x1080)
5. Generating thumbnails (300x200px)
6. Saving all images to `docs/screenshots/`

## Quick Start

### Prerequisites

- **.NET 10 SDK** installed
- **InternalAI frontend** running at `http://localhost:3000`
- ~100MB disk space for Playwright browser binaries (first run only)

### Manual Capture

1. **Start InternalAI frontend:**
   ```powershell
   cd C:\git\InternalAI\frontend
   npm start
   ```

2. **Run the capture tool:**
   ```powershell
   cd C:\git\API-first-LLM-orchestration\tools
   .\Capture-Screenshots.ps1
   ```

3. **Review and commit:**
   ```powershell
   # Review screenshots
   explorer ..\docs\screenshots

   # Commit changes
   git add ..\docs\screenshots\*.png
   git commit -m "Add/update GUI screenshots"
   git push
   ```

### Automated Capture (GitHub Actions)

The repository includes a GitHub Actions workflow that can capture screenshots automatically:

**Manual Trigger:**
1. Go to GitHub repository → Actions tab
2. Select "Update GUI Screenshots" workflow
3. Click "Run workflow"
4. Workflow will:
   - Checkout both repositories
   - Start InternalAI frontend
   - Capture all screenshots
   - Commit and push changes automatically

**Scheduled Trigger:**
- Workflow runs weekly on Sundays at 2 AM UTC (can be disabled in `.github/workflows/update-screenshots.yml`)

## How It Works

### 1. Configuration

The tool reads `src/ApiFirst.LlmOrchestration.McpServer/GuiSupportMappings.json`:

```json
{
  "mappings": [
    {
      "operationId": "GetTeamMembers",
      "guiRoute": "/team",
      "screenshotUrl": "team-list.png",
      "thumbnailUrl": "team-list-thumb.png"
    }
  ]
}
```

### 2. Browser Automation

Uses **Microsoft Playwright** for .NET:
- Launches headless Chromium
- Sets viewport to 1920x1080
- Navigates to each route
- Waits for page to load fully
- Captures screenshot

### 3. Image Processing

Uses **SixLabors.ImageSharp**:
- Loads full screenshot
- Resizes to 300x200 (center crop)
- Saves as PNG thumbnail

### 4. Parameter Handling

For parameterized routes like `/team/{id}`:
- Automatically replaces `{id}` with `1`
- Uses first available record for screenshots
- Avoids duplicate captures for same route

## Project Structure

```
tools/
├── Capture-Screenshots.ps1           # PowerShell wrapper script
└── ScreenshotCapture/
    ├── ScreenshotCapture.csproj      # .NET project file
    └── Program.cs                    # Main capture logic
```

## Command-Line Options

### PowerShell Script

```powershell
# Default usage
.\Capture-Screenshots.ps1

# Custom GUI URL
.\Capture-Screenshots.ps1 -GuiUrl http://localhost:8080

# Skip build step (faster if already built)
.\Capture-Screenshots.ps1 -Build $false
```

### Direct .NET Execution

```powershell
# Build
dotnet build ScreenshotCapture/ScreenshotCapture.csproj

# Run
dotnet run --project ScreenshotCapture/ScreenshotCapture.csproj
```

## Output

Screenshots are saved to `docs/screenshots/`:

```
docs/screenshots/
├── login.png                    (1920x1080)
├── login-thumb.png              (300x200)
├── team-list.png
├── team-list-thumb.png
├── team-detail.png
├── team-detail-thumb.png
├── courses-list.png
├── courses-list-thumb.png
└── ... (other GUI features)
```

## Troubleshooting

### "InternalAI frontend is not running"

**Solution:**
```powershell
cd C:\git\InternalAI\frontend
npm install  # if first time
npm start
```

Wait for "webpack compiled successfully" message before running capture tool.

### "Playwright browsers not installed"

**Solution:**
The tool will automatically install Chromium on first run. If it fails:
```powershell
dotnet tool install --global Microsoft.Playwright.CLI
playwright install chromium
```

### "Screenshot capture failed"

Check that:
- InternalAI is running and accessible
- You have internet connection (for first-time Playwright download)
- Port 3000 is not blocked by firewall
- You have write permissions to `docs/screenshots/`

### "Thumbnails look distorted"

The tool uses center-crop to maintain 3:2 aspect ratio. If the source page has a very different aspect ratio, some content may be cropped. This is intentional to keep thumbnail sizes consistent.

## Advanced Usage

### Capture Specific Routes Only

Modify `GuiSupportMappings.json` to temporarily remove mappings you don't want to capture, or create a custom mappings file:

```csharp
// In Program.cs, change:
var mappingsPath = Path.Combine(repoRoot, "custom-mappings.json");
```

### Change Screenshot Resolution

```csharp
// In Program.cs, modify:
private const int FullWidth = 2560;   // 2K resolution
private const int FullHeight = 1440;
```

### Add Authentication

If GUI routes require login, add authentication to the browser context:

```csharp
// In Program.cs, before capturing screenshots:
await page.GotoAsync(GuiBaseUrl + "/login");
await page.FillAsync("input[name='username']", "admin");
await page.FillAsync("input[name='password']", "Admin1234!");
await page.ClickAsync("button[type='submit']");
await page.WaitForNavigationAsync();
```

## Integration with CI/CD

### GitHub Actions

The included workflow (`.github/workflows/update-screenshots.yml`) automatically:
- Runs on manual trigger or weekly schedule
- Checks out both repositories
- Starts InternalAI frontend
- Captures screenshots
- Commits and pushes changes

### Azure DevOps

Create a pipeline YAML:

```yaml
trigger: none

pool:
  vmImage: 'windows-latest'

steps:
- task: UseDotNet@2
  inputs:
    version: '10.x'

- task: NodeTool@0
  inputs:
    versionSpec: '20.x'

- checkout: self
- checkout: git://InternalAI@main

- script: npm ci
  workingDirectory: InternalAI/frontend

- powershell: |
    Start-Process powershell -ArgumentList "-Command npm start" -PassThru
    Start-Sleep -Seconds 30
  workingDirectory: InternalAI/frontend

- script: dotnet run --project tools/ScreenshotCapture/ScreenshotCapture.csproj

- script: |
    git config user.name "Azure DevOps"
    git add docs/screenshots/*.png
    git commit -m "Update screenshots [skip ci]"
    git push
```

## Dependencies

- **Microsoft.Playwright** (1.49.0): Browser automation
- **SixLabors.ImageSharp** (3.1.5): Image processing and thumbnail generation
- **System.Text.Json** (10.0.0): JSON parsing

## License

Same as parent repository.

## Maintenance

When GUI features are added or changed:
1. Update `GuiSupportMappings.json`
2. Run `.\Capture-Screenshots.ps1`
3. Review and commit new screenshots
4. Screenshots will automatically be available via GitHub to MCP clients
