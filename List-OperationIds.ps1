# List All Operation IDs from Swagger
# Run this to see all available operation IDs for your test mappings

Write-Host "Fetching Operation IDs from Swagger..." -ForegroundColor Cyan
Write-Host ""

# Check appsettings for Swagger URL
$appsettingsPath = "src\ApiFirst.LlmOrchestration.McpServer\appsettings.json"

if (Test-Path $appsettingsPath) {
    $config = Get-Content $appsettingsPath | ConvertFrom-Json
    $swaggerUrl = $config.McpServer.DefaultSwaggerUrl

    Write-Host "Swagger URL: $swaggerUrl" -ForegroundColor Yellow
    Write-Host ""

    if ($swaggerUrl) {
        Write-Host "Attempting to fetch operations..." -ForegroundColor Gray
        Write-Host ""

        try {
            $swagger = Invoke-RestMethod -Uri $swaggerUrl -ErrorAction Stop

            Write-Host "=== AVAILABLE OPERATION IDs ===" -ForegroundColor Green
            Write-Host ""

            $operations = @()

            foreach ($path in $swagger.paths.PSObject.Properties) {
                $pathName = $path.Name

                foreach ($method in $path.Value.PSObject.Properties) {
                    $methodName = $method.Name.ToUpper()
                    $operation = $method.Value

                    if ($operation.operationId) {
                        $operations += [PSCustomObject]@{
                            OperationId = $operation.operationId
                            Method = $methodName
                            Path = $pathName
                            Summary = $operation.summary
                        }
                    }
                }
            }

            # Sort by path
            $operations | Sort-Object Path | Format-Table -AutoSize

            Write-Host ""
            Write-Host "Total Operations: $($operations.Count)" -ForegroundColor Cyan
            Write-Host ""
            Write-Host "=== USAGE IN TestMappings.json ===" -ForegroundColor Yellow
            Write-Host ""
            Write-Host 'Example:' -ForegroundColor Gray
            Write-Host '{' -ForegroundColor Gray
            Write-Host '  "testId": "test/test_team_api.py::test_your_test",' -ForegroundColor Gray
            Write-Host '  "operations": ["Login", "GetTeamMembers"],' -ForegroundColor Gray
            Write-Host '  "capabilities": ["login", "getteammembers"]' -ForegroundColor Gray
            Write-Host '}' -ForegroundColor Gray

        }
        catch {
            Write-Host "Could not fetch Swagger from $swaggerUrl" -ForegroundColor Red
            Write-Host "Error: $_" -ForegroundColor Red
            Write-Host ""
            Write-Host "Make sure your API is running at the configured URL" -ForegroundColor Yellow
            Write-Host ""
            Write-Host "Alternatively, start MCP server and use:" -ForegroundColor Cyan
            Write-Host '  "List all API operations"' -ForegroundColor Gray
        }
    }
}
else {
    Write-Host "appsettings.json not found" -ForegroundColor Red
}

Write-Host ""
Write-Host "See OPERATION_ID_GUIDE.md for common patterns" -ForegroundColor Cyan
