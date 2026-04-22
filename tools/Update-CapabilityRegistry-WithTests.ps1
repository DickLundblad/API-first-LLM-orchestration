# Update-CapabilityRegistry-WithTests.ps1
# Merges test analysis data into the capability registry and updates status

param(
    [string]$TestAnalysisPath = ".\api-test-analysis.json",
    [string]$CapabilityRegistryPath = "src\ApiFirst.LlmOrchestration.McpServer\CapabilityRegistry.json",
    [int]$MinTestsForApiVerified = 3,
    [switch]$DryRun
)

Write-Host "Updating Capability Registry with test data..." -ForegroundColor Cyan
Write-Host ""

# Load test analysis
if (-not (Test-Path $TestAnalysisPath)) {
    Write-Error "Test analysis not found at: $TestAnalysisPath"
    Write-Host "Run Analyze-ApiTests.ps1 first to generate the analysis" -ForegroundColor Yellow
    exit 1
}

$testAnalysis = Get-Content $TestAnalysisPath -Raw | ConvertFrom-Json
Write-Host "✓ Loaded test analysis with $($testAnalysis.Summary.TotalTests) tests" -ForegroundColor Green

# Load capability registry
if (-not (Test-Path $CapabilityRegistryPath)) {
    Write-Error "Capability registry not found at: $CapabilityRegistryPath"
    exit 1
}

$capabilities = Get-Content $CapabilityRegistryPath -Raw | ConvertFrom-Json
Write-Host "✓ Loaded capability registry with $($capabilities.Count) capabilities" -ForegroundColor Green
Write-Host ""

$updated = 0
$statusChanged = 0

foreach ($capability in $capabilities) {
    Write-Host "Processing: $($capability.id) - $($capability.name)" -ForegroundColor Yellow

    # Get all operation IDs for this capability
    $opsToCheck = if ($capability.apiOperationIds -is [string]) { 
        @($capability.apiOperationIds) 
    } else { 
        $capability.apiOperationIds 
    }

    # Collect all test IDs that test any of these operations
    $allTestIds = @()
    $operationCoverage = @{}

    foreach ($opId in $opsToCheck) {
        if ($testAnalysis.Operations.$opId) {
            $tests = $testAnalysis.Operations.$opId.Tests
            $operationCoverage[$opId] = $tests.Count
            foreach ($test in $tests) {
                if ($test -notin $allTestIds) {
                    $allTestIds += $test
                }
            }
            Write-Host "  $opId`: $($tests.Count) tests" -ForegroundColor Green
        }
        else {
            $operationCoverage[$opId] = 0
            Write-Host "  $opId`: 0 tests" -ForegroundColor Red
        }
    }

    # Update apiTestIds
    $currentTestIds = if ($capability.apiTestIds) { 
        if ($capability.apiTestIds -is [string]) { @($capability.apiTestIds) } else { $capability.apiTestIds }
    } else { 
        @() 
    }

    if ($allTestIds.Count -gt 0) {
        # Merge new test IDs with existing ones
        $mergedTestIds = @($currentTestIds) + @($allTestIds) | Select-Object -Unique | Sort-Object

        if ($mergedTestIds.Count -ne $currentTestIds.Count -or 
            (Compare-Object $mergedTestIds $currentTestIds -SyncWindow 0)) {
            $capability.apiTestIds = $mergedTestIds
            Write-Host "  Updated apiTestIds: $($currentTestIds.Count) → $($mergedTestIds.Count)" -ForegroundColor Cyan
            $updated++
        }
        else {
            Write-Host "  apiTestIds unchanged: $($currentTestIds.Count) tests" -ForegroundColor DarkGray
        }
    }

    # Determine status based on test coverage
    $currentStatus = $capability.status
    $newStatus = $currentStatus

    $totalTests = $allTestIds.Count
    $opsWithTests = ($operationCoverage.Values | Where-Object { $_ -gt 0 }).Count
    $totalOps = $opsToCheck.Count

    if ($totalTests -ge $MinTestsForApiVerified -and $opsWithTests -eq $totalOps) {
        # All operations have tests and minimum threshold met
        $newStatus = "ApiVerified"
    }
    elseif ($totalTests -gt 0) {
        # Some operations have tests
        $newStatus = "ApiImplemented"
    }
    elseif ($totalOps -gt 0) {
        # Operations exist but no tests
        $newStatus = "InProgress"
    }
    else {
        $newStatus = "Planned"
    }

    if ($newStatus -ne $currentStatus) {
        $capability.status = $newStatus
        Write-Host "  Status: $currentStatus → $newStatus" -ForegroundColor Magenta
        $statusChanged++
    }
    else {
        Write-Host "  Status: $currentStatus (unchanged)" -ForegroundColor DarkGray
    }

    Write-Host "  Coverage: $opsWithTests/$totalOps operations, $totalTests total tests" -ForegroundColor Gray
    Write-Host ""
}

# Summary
Write-Host "=" * 80 -ForegroundColor Cyan
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Total capabilities: $($capabilities.Count)" -ForegroundColor White
Write-Host "  Updated with test IDs: $updated" -ForegroundColor $(if ($updated -gt 0) { "Yellow" } else { "White" })
Write-Host "  Status changed: $statusChanged" -ForegroundColor $(if ($statusChanged -gt 0) { "Yellow" } else { "White" })
Write-Host "=" * 80 -ForegroundColor Cyan
Write-Host ""

# Show status distribution
$statusGroups = $capabilities | Group-Object -Property status
Write-Host "Status Distribution:" -ForegroundColor Cyan
foreach ($group in $statusGroups | Sort-Object Name) {
    $color = switch ($group.Name) {
        "ApiVerified" { "Green" }
        "ApiImplemented" { "Yellow" }
        "InProgress" { "Cyan" }
        default { "Gray" }
    }
    Write-Host "  $($group.Name): $($group.Count)" -ForegroundColor $color
}
Write-Host ""

# Save updated registry
if ($updated -gt 0 -or $statusChanged -gt 0) {
    if ($DryRun) {
        Write-Host "⚠ DRY RUN - No changes saved" -ForegroundColor Yellow
        Write-Host "Run without -DryRun to save changes" -ForegroundColor Gray
    }
    else {
        # Convert back to JSON with proper formatting
        $json = $capabilities | ConvertTo-Json -Depth 10
        $json | Set-Content $CapabilityRegistryPath -Encoding UTF8
        Write-Host "✅ Updated capability registry saved to: $CapabilityRegistryPath" -ForegroundColor Green
        Write-Host ""
        Write-Host "Next steps:" -ForegroundColor Cyan
        Write-Host "1. Rebuild the MCP Server project: dotnet build" -ForegroundColor White
        Write-Host "2. Restart the MCP Server to load the updated registry" -ForegroundColor White
    }
}
else {
    Write-Host "ℹ No updates needed - registry is up to date" -ForegroundColor Blue
}
