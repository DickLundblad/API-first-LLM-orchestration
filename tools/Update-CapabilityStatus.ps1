# Update-CapabilityStatus.ps1
# Updates capability status by validating against Swagger/OpenAPI document

param(
    [string]$SwaggerUrl = "http://localhost:5000/api/swagger.json",
    [string]$CapabilityRegistryPath = "src\ApiFirst.LlmOrchestration.McpServer\CapabilityRegistry.json",
    [switch]$DryRun
)

Write-Host "Validating capabilities against Swagger document..." -ForegroundColor Cyan
Write-Host "Swagger URL: $SwaggerUrl" -ForegroundColor Gray
Write-Host ""

# Load the capability registry
if (-not (Test-Path $CapabilityRegistryPath)) {
    Write-Error "Capability registry not found at: $CapabilityRegistryPath"
    exit 1
}

$capabilities = Get-Content $CapabilityRegistryPath -Raw | ConvertFrom-Json

# Fetch the Swagger document
try {
    $swagger = Invoke-RestMethod -Uri $SwaggerUrl -Method Get -ErrorAction Stop
    Write-Host "✓ Successfully loaded Swagger document" -ForegroundColor Green
    Write-Host "  API Title: $($swagger.info.title)" -ForegroundColor Gray
    Write-Host "  Version: $($swagger.info.version)" -ForegroundColor Gray
    Write-Host ""
}
catch {
    Write-Error "Failed to fetch Swagger document from $SwaggerUrl"
    Write-Error $_.Exception.Message
    exit 1
}

# Extract all operation IDs from Swagger
$swaggerOperations = @{}
foreach ($path in $swagger.paths.PSObject.Properties) {
    foreach ($method in $path.Value.PSObject.Properties) {
        if ($method.Value.operationId) {
            $swaggerOperations[$method.Value.operationId] = @{
                Path = $path.Name
                Method = $method.Name.ToUpper()
                Summary = $method.Value.summary
                Tags = $method.Value.tags
            }
        }
    }
}

Write-Host "Found $($swaggerOperations.Count) operations in Swagger document" -ForegroundColor Cyan
Write-Host ""

# Validate each capability
$updated = 0
$alreadyCorrect = 0

foreach ($capability in $capabilities) {
    Write-Host "Checking: $($capability.id) - $($capability.name)" -ForegroundColor Yellow

    $missingOps = @()
    $foundOps = @()
    $allOpsFound = $true

    # Check each API operation
    $opsToCheck = if ($capability.apiOperationIds -is [string]) { 
        @($capability.apiOperationIds) 
    } else { 
        $capability.apiOperationIds 
    }

    foreach ($opId in $opsToCheck) {
        if ($swaggerOperations.ContainsKey($opId)) {
            $foundOps += $opId
            $op = $swaggerOperations[$opId]
            Write-Host "  ✓ $opId - $($op.Method) $($op.Path)" -ForegroundColor Green
        }
        else {
            $missingOps += $opId
            $allOpsFound = $false
            Write-Host "  ✗ $opId - NOT FOUND in Swagger" -ForegroundColor Red
        }
    }

    # Determine new status
    $currentStatus = $capability.status
    $newStatus = if ($allOpsFound -and $foundOps.Count -gt 0) {
        "ApiImplemented"  # All operations exist in Swagger
    }
    elseif ($foundOps.Count -gt 0) {
        "InProgress"  # Some operations exist
    }
    else {
        "Planned"  # No operations found
    }

    # Update status if changed
    if ($currentStatus -ne $newStatus) {
        Write-Host "  Status: $currentStatus → $newStatus" -ForegroundColor Cyan
        $capability.status = $newStatus
        $updated++
    }
    else {
        Write-Host "  Status: $currentStatus (unchanged)" -ForegroundColor DarkGray
        $alreadyCorrect++
    }

    Write-Host ""
}

# Summary
Write-Host "=" * 80 -ForegroundColor Cyan
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Total capabilities: $($capabilities.Count)" -ForegroundColor White
Write-Host "  Updated: $updated" -ForegroundColor $(if ($updated -gt 0) { "Yellow" } else { "White" })
Write-Host "  Already correct: $alreadyCorrect" -ForegroundColor White
Write-Host "=" * 80 -ForegroundColor Cyan

# Save updated registry
if ($updated -gt 0) {
    if ($DryRun) {
        Write-Host "`n⚠ DRY RUN - No changes saved" -ForegroundColor Yellow
        Write-Host "Run without -DryRun to save changes" -ForegroundColor Gray
    }
    else {
        # Convert back to JSON with proper formatting
        $json = $capabilities | ConvertTo-Json -Depth 10
        $json | Set-Content $CapabilityRegistryPath -Encoding UTF8
        Write-Host "`n✅ Updated capability registry saved to: $CapabilityRegistryPath" -ForegroundColor Green
        Write-Host "`nNext steps:" -ForegroundColor Cyan
        Write-Host "1. Rebuild the MCP Server project to deploy the updated registry" -ForegroundColor White
        Write-Host "2. Restart the MCP Server to load the new status values" -ForegroundColor White
    }
}
else {
    Write-Host "`nℹ No updates needed - all statuses are already correct" -ForegroundColor Blue
}
