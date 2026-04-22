# Script to help generate CapabilityRegistry.json from GuiSupportMappings.json
# Run from: src/ApiFirst.LlmOrchestration.McpServer/

param(
    [string]$GuiMappingsPath = "GuiSupportMappings.json",
    [string]$OutputPath = "CapabilityRegistry-generated.json"
)

Write-Host "Reading GUI Support Mappings..." -ForegroundColor Cyan
$guiMappings = Get-Content $GuiMappingsPath | ConvertFrom-Json

# Group operations by GUI route (this identifies logical capabilities)
$grouped = $guiMappings.mappings | Group-Object -Property guiRoute

Write-Host "`nFound $($grouped.Count) potential capabilities based on GUI routes:`n" -ForegroundColor Green

$capabilities = @()

foreach ($group in $grouped) {
    $route = $group.Name
    $operations = $group.Group

    # Generate capability ID from route
    $capabilityId = $route -replace '^/', '' -replace '/', '-' -replace '\{.*?\}', 'detail'
    if ([string]::IsNullOrEmpty($capabilityId)) { $capabilityId = "root" }

    # Get all operation IDs - ensure it's always an array
    $operationIds = @($operations | ForEach-Object { $_.operationId })

    # Infer category from route
    $category = "General"
    if ($route -match '/team') { $category = "Team" }
    elseif ($route -match '/course') { $category = "Courses" }
    elseif ($route -match '/enroll') { $category = "Enrollment" }

    # Create capability with explicit array types
    $capability = [ordered]@{
        id = $capabilityId
        name = ($operations[0].guiFeature -split ' - ')[0]  # Use first operation's feature name
        description = "Manage $($operations[0].guiFeature.ToLower()) via API"
        category = $category
        status = "ApiVerified"  # TODO: Update based on actual implementation status
        apiOperationIds = @($operationIds)  # Ensure array
        apiTestIds = @()  # TODO: Add your test IDs here
        guiRoute = $route
        guiFeature = $operations[0].guiFeature
        guiTestIds = @()  # TODO: Add your GUI test IDs here
        backlogItemIds = @()  # TODO: Add your backlog item IDs here
        requiredEvidenceLevel = "ApiTests"
        metadata = [ordered]@{
            owner = "TODO"
            priority = "Medium"
            generatedFrom = "GuiSupportMappings"
        }
    }

    $capabilities += $capability

    Write-Host "  [$category] $capabilityId" -ForegroundColor Yellow
    Write-Host "    - Operations: $($operationIds -join ', ')" -ForegroundColor Gray
    Write-Host "    - GUI: $route" -ForegroundColor Gray
    Write-Host ""
}

# Convert to JSON
$json = $capabilities | ConvertTo-Json -Depth 10

Write-Host "Writing to $OutputPath..." -ForegroundColor Cyan
$json | Out-File -FilePath $OutputPath -Encoding UTF8

Write-Host "`n✅ Generated $($capabilities.Count) capabilities!" -ForegroundColor Green
Write-Host "`nNext steps:" -ForegroundColor Cyan
Write-Host "1. Review $OutputPath" -ForegroundColor White
Write-Host "2. Update 'status' based on actual implementation (Planned/InProgress/ApiImplemented/ApiVerified/FullyVerified)" -ForegroundColor White
Write-Host "3. Add 'apiTestIds' - your actual test class.method names" -ForegroundColor White
Write-Host "4. Add 'guiTestIds' if you have GUI tests" -ForegroundColor White
Write-Host "5. Add 'backlogItemIds' from your issue tracker" -ForegroundColor White
Write-Host "6. Update 'metadata.owner' with team/squad name" -ForegroundColor White
Write-Host "7. Update 'metadata.priority' (High/Medium/Low)" -ForegroundColor White
Write-Host "8. Rename to CapabilityRegistry.json when ready" -ForegroundColor White

# API Surface (Swagger/OpenAPI)
#        ↓
# Capabilities (traceable operations)
#        ↓
# Evidence (API tests, UI tests, runtime validation)
#        ↓
# Verified Capabilities
#        ↓
# Business Use Cases (built on verified capabilities)
