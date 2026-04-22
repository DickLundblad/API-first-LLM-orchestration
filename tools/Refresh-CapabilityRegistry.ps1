# Refresh-CapabilityRegistry.ps1
# Complete workflow to refresh the capability registry with current data

param(
    [string]$InternalAIPath = "C:\git\InternalAI",
    [string]$SwaggerUrl = "http://localhost:5000/api/swagger.json",
    [switch]$SkipTestAnalysis,
    [switch]$SkipSwaggerValidation,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$scriptDir = $PSScriptRoot

Write-Host ""
Write-Host "=" * 80 -ForegroundColor Cyan
Write-Host "Capability Registry Refresh Workflow" -ForegroundColor Cyan
Write-Host "=" * 80 -ForegroundColor Cyan
Write-Host ""

# Step 1: Analyze tests (if not skipped)
if (-not $SkipTestAnalysis) {
    Write-Host "Step 1: Analyzing API tests..." -ForegroundColor Yellow
    Write-Host ""

    if (Test-Path $InternalAIPath) {
        & "$scriptDir\Analyze-ApiTests.ps1" -InternalAIPath $InternalAIPath -OutputPath "$scriptDir\api-test-analysis.json"
        Write-Host ""
    }
    else {
        Write-Warning "InternalAI path not found: $InternalAIPath"
        Write-Warning "Skipping test analysis"
        $SkipTestAnalysis = $true
    }
}

# Step 2: Update with test data (if analysis was done)
if (-not $SkipTestAnalysis -and (Test-Path "$scriptDir\api-test-analysis.json")) {
    Write-Host "Step 2: Updating registry with test data..." -ForegroundColor Yellow
    Write-Host ""

    & "$scriptDir\Update-CapabilityRegistry-WithTests.ps1" `
        -TestAnalysisPath "$scriptDir\api-test-analysis.json" `
        -CapabilityRegistryPath "$scriptDir\..\src\ApiFirst.LlmOrchestration.McpServer\CapabilityRegistry.json" `
        -DryRun:$DryRun

    Write-Host ""
}

# Step 3: Validate against Swagger (if not skipped)
if (-not $SkipSwaggerValidation) {
    Write-Host "Step 3: Validating against Swagger document..." -ForegroundColor Yellow
    Write-Host ""

    try {
        & "$scriptDir\Update-CapabilityStatus.ps1" `
            -SwaggerUrl $SwaggerUrl `
            -CapabilityRegistryPath "$scriptDir\..\src\ApiFirst.LlmOrchestration.McpServer\CapabilityRegistry.json" `
            -DryRun:$DryRun

        Write-Host ""
    }
    catch {
        Write-Warning "Swagger validation failed: $($_.Exception.Message)"
        Write-Warning "Make sure the API is running at $SwaggerUrl"
    }
}

# Step 4: Rebuild if changes were made
if (-not $DryRun) {
    Write-Host "Step 4: Rebuilding MCP Server..." -ForegroundColor Yellow
    Write-Host ""

    $mcpProject = "$scriptDir\..\src\ApiFirst.LlmOrchestration.McpServer\ApiFirst.LlmOrchestration.McpServer.csproj"

    dotnet build $mcpProject

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "✅ Build successful!" -ForegroundColor Green
    }
    else {
        Write-Error "Build failed"
        exit $LASTEXITCODE
    }
}

Write-Host ""
Write-Host "=" * 80 -ForegroundColor Cyan
Write-Host "Workflow Complete!" -ForegroundColor Green
Write-Host "=" * 80 -ForegroundColor Cyan
Write-Host ""

if ($DryRun) {
    Write-Host "This was a DRY RUN - no changes were saved" -ForegroundColor Yellow
    Write-Host "Run without -DryRun to apply changes" -ForegroundColor Yellow
}
else {
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "1. Restart the MCP Server to load the updated registry" -ForegroundColor White
    Write-Host "2. Test with: list_capabilities MCP tool" -ForegroundColor White
    Write-Host "3. Review status changes and test coverage" -ForegroundColor White
}
Write-Host ""
