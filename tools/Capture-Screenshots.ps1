#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Automatically captures screenshots of InternalAI GUI features.

.DESCRIPTION
    This script runs the automated screenshot capture tool that:
    1. Reads GUI mappings from GuiSupportMappings.json
    2. Launches a headless browser
    3. Navigates to each GUI feature
    4. Captures full-size screenshots
    5. Generates thumbnails (300x200px)
    6. Saves to docs/screenshots/

.PARAMETER GuiUrl
    The URL where InternalAI frontend is running (default: http://localhost:3000)

.PARAMETER Build
    Build the tool before running (default: true)

.EXAMPLE
    .\Capture-Screenshots.ps1
    Captures all screenshots using default settings

.EXAMPLE
    .\Capture-Screenshots.ps1 -GuiUrl http://localhost:8080
    Captures screenshots from a different port

.NOTES
    Prerequisites:
    - InternalAI frontend must be running
    - .NET 10 SDK installed
    - First run will download Playwright browser binaries (~100MB)
#>

param(
    [string]$GuiUrl = "http://localhost:3000",
    [bool]$Build = $true
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$toolProject = Join-Path $PSScriptRoot "ScreenshotCapture\ScreenshotCapture.csproj"
$screenshotsDir = Join-Path $repoRoot "docs\screenshots"

Write-Host "InternalAI GUI Screenshot Capture" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host

# Check if InternalAI is running
Write-Host "Checking if InternalAI frontend is running at $GuiUrl..." -NoNewline
try {
    $response = Invoke-WebRequest -Uri $GuiUrl -Method Head -TimeoutSec 5 -ErrorAction Stop
    Write-Host " ✓" -ForegroundColor Green
}
catch {
    Write-Host " ✗" -ForegroundColor Red
    Write-Host
    Write-Host "Error: InternalAI frontend is not running at $GuiUrl" -ForegroundColor Red
    Write-Host
    Write-Host "To start InternalAI frontend:" -ForegroundColor Yellow
    Write-Host "  cd C:\git\InternalAI\frontend" -ForegroundColor Yellow
    Write-Host "  npm start" -ForegroundColor Yellow
    Write-Host
    exit 1
}

# Build the tool if requested
if ($Build) {
    Write-Host "Building screenshot capture tool..." -NoNewline
    $buildOutput = dotnet build $toolProject --configuration Release 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host " ✗" -ForegroundColor Red
        Write-Host
        Write-Host "Build failed:" -ForegroundColor Red
        Write-Host $buildOutput
        exit 1
    }
    Write-Host " ✓" -ForegroundColor Green
}

# Run the tool
Write-Host
Write-Host "Starting screenshot capture..." -ForegroundColor Cyan
Write-Host

$toolExe = Join-Path $PSScriptRoot "ScreenshotCapture\bin\Release\net10.0\ScreenshotCapture.exe"
if (Test-Path $toolExe) {
    & $toolExe
    $exitCode = $LASTEXITCODE
}
else {
    # Fallback to dotnet run
    dotnet run --project $toolProject --configuration Release --no-build
    $exitCode = $LASTEXITCODE
}

if ($exitCode -eq 0) {
    Write-Host
    Write-Host "Success! Screenshots captured to:" -ForegroundColor Green
    Write-Host "  $screenshotsDir" -ForegroundColor Green
    Write-Host
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "1. Review screenshots: explorer $screenshotsDir" -ForegroundColor Yellow
    Write-Host "2. Commit changes:" -ForegroundColor Yellow
    Write-Host "   git add docs/screenshots/*.png" -ForegroundColor Gray
    Write-Host "   git commit -m 'Add GUI feature screenshots'" -ForegroundColor Gray
    Write-Host "   git push" -ForegroundColor Gray
    Write-Host
}
else {
    Write-Host
    Write-Host "Screenshot capture failed with exit code $exitCode" -ForegroundColor Red
    exit $exitCode
}
