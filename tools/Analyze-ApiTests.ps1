# Analyze-ApiTests.ps1
# Analyzes Python test files to extract API endpoints, methods, and test coverage

param(
    [string]$InternalAIPath = "C:\git\InternalAI",
    [string]$OutputPath = ".\api-test-analysis.json",
    [switch]$Verbose
)

# Helper function to map endpoint to operation ID
function Get-OperationFromEndpoint {
    param($Endpoint, $Method)

    # Common patterns
    $patterns = @{
        '^/api/team$' = @{ GET = 'GetTeamMembers' }
        '^/api/team/(\d+)$' = @{ GET = 'GetTeamMember'; PUT = 'UpdateTeamMember'; PATCH = 'UpdateTeamMember'; DELETE = 'DeleteTeamMember' }
        '^/api/courses$' = @{ GET = 'GetCourses'; POST = 'CreateCourse' }
        '^/api/courses/(\d+)$' = @{ GET = 'GetCourse'; PUT = 'UpdateCourse'; DELETE = 'DeleteCourse' }
        '^/api/courses/(\d+)/approve$' = @{ POST = 'ApproveCourse'; PUT = 'ApproveCourse' }
        '^/api/courses/(\d+)/archive$' = @{ POST = 'ArchiveCourse'; PUT = 'ArchiveCourse'; PATCH = 'ArchiveCourse' }
        '^/api/consultants/(\d+)/courses$' = @{ GET = 'GetConsultantCourses'; POST = 'EnrollCourse' }
        '^/api/auth/login$' = @{ POST = 'Login' }
        '^/api/auth/forgot-password$' = @{ POST = 'ForgotPassword' }
        '^/api/auth/reset-password$' = @{ POST = 'ResetPassword' }
        '^/api/auth/verify-email$' = @{ POST = 'VerifyEmail'; GET = 'VerifyEmail' }
    }

    foreach ($pattern in $patterns.Keys) {
        if ($Endpoint -match $pattern) {
            $methodMap = $patterns[$pattern]
            if ($methodMap.ContainsKey($Method)) {
                return $methodMap[$Method]
            }
        }
    }

    return $null
}

# Helper function to describe what test does
function Get-TestDescription {
    param($TestName, $Docstring)

    if ($Docstring) {
        return $Docstring.Trim()
    }

    # Infer from test name
    $name = $TestName -replace '_', ' '
    return $name
}

Write-Host "Analyzing API tests in $InternalAIPath..." -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $InternalAIPath)) {
    Write-Error "InternalAI repository not found at: $InternalAIPath"
    exit 1
}

# Find all Python test files
$testFiles = Get-ChildItem -Path $InternalAIPath -Filter "test_*.py" -Recurse | 
    Where-Object { $_.FullName -notlike "*\.venv\*" -and $_.FullName -notlike "*\venv\*" -and $_.FullName -notlike "*\node_modules\*" }

if ($testFiles.Count -eq 0) {
    Write-Warning "No Python test files found in $InternalAIPath"

    # Try alternate patterns
    $testFiles = Get-ChildItem -Path $InternalAIPath -Filter "*_test.py" -Recurse | 
        Where-Object { $_.FullName -notlike "*\.venv\*" -and $_.FullName -notlike "*\venv\*" }

    if ($testFiles.Count -eq 0) {
        Write-Error "No test files found"
        exit 1
    }
}

Write-Host "Found $($testFiles.Count) test files to analyze" -ForegroundColor Green
Write-Host ""

$testAnalysis = @()
$endpointMap = @{}
$operationMap = @{}

foreach ($file in $testFiles) {
    Write-Host "Analyzing: $($file.Name)" -ForegroundColor Yellow

    $content = Get-Content $file.FullName -Raw
    $relativePath = $file.FullName.Replace($InternalAIPath, "").TrimStart("\", "/")

    # Extract test functions
    $testFunctions = [regex]::Matches($content, '(?ms)def\s+(test_\w+)\s*\([^)]*\):\s*(?:"""([^"]+)""")?\s*(.{0,500})')

    foreach ($match in $testFunctions) {
        $testName = $match.Groups[1].Value
        $docstring = $match.Groups[2].Value
        $bodySnippet = $match.Groups[3].Value

        $testInfo = @{
            File = $file.Name
            RelativePath = $relativePath
            TestName = $testName
            Docstring = $docstring
            Endpoints = @()
            Methods = @()
            Operations = @()
            Description = ""
        }

        # Extract API endpoints being called
        # Patterns: client.get("/api/..."), client.post("/api/..."), etc.
        $endpointCalls = [regex]::Matches($content, 'client\.(get|post|put|patch|delete)\s*\(\s*[''"]([^''"]+)[''"]')

        foreach ($call in $endpointCalls) {
            $method = $call.Groups[1].Value.ToUpper()
            $endpoint = $call.Groups[2].Value

            if ($endpoint -notin $testInfo.Endpoints) {
                $testInfo.Endpoints += $endpoint
            }
            if ($method -notin $testInfo.Methods) {
                $testInfo.Methods += $method
            }

            # Map endpoint to potential operation ID
            $operation = Get-OperationFromEndpoint -Endpoint $endpoint -Method $method
            if ($operation -and $operation -notin $testInfo.Operations) {
                $testInfo.Operations += $operation
            }

            # Track endpoint usage
            $endpointKey = "$method $endpoint"
            if (-not $endpointMap.ContainsKey($endpointKey)) {
                $endpointMap[$endpointKey] = @{
                    Method = $method
                    Endpoint = $endpoint
                    Operation = $operation
                    Tests = @()
                }
            }
            $endpointMap[$endpointKey].Tests += "$($file.Name)::$testName"
        }

        # Try to infer what's being tested from test name and docstring
        $testInfo.Description = Get-TestDescription -TestName $testName -Docstring $docstring

        # Track operation coverage
        foreach ($op in $testInfo.Operations) {
            if (-not $operationMap.ContainsKey($op)) {
                $operationMap[$op] = @{
                    Operation = $op
                    Tests = @()
                    Endpoints = @()
                }
            }
            $testId = "$($file.Name)::$testName"
            if ($testId -notin $operationMap[$op].Tests) {
                $operationMap[$op].Tests += $testId
            }
        }

        if ($Verbose) {
            Write-Host "  $testName" -ForegroundColor Gray
            if ($testInfo.Endpoints.Count -gt 0) {
                Write-Host "    Endpoints: $($testInfo.Endpoints -join ', ')" -ForegroundColor DarkGray
            }
            if ($testInfo.Operations.Count -gt 0) {
                Write-Host "    Operations: $($testInfo.Operations -join ', ')" -ForegroundColor DarkCyan
            }
        }

        $testAnalysis += [PSCustomObject]$testInfo
    }
}

Write-Host ""
Write-Host "=" * 80 -ForegroundColor Cyan
Write-Host "Analysis Summary:" -ForegroundColor Cyan
Write-Host "  Total tests analyzed: $($testAnalysis.Count)" -ForegroundColor White
Write-Host "  Unique endpoints found: $($endpointMap.Count)" -ForegroundColor White
Write-Host "  Operations with test coverage: $($operationMap.Count)" -ForegroundColor White
Write-Host "=" * 80 -ForegroundColor Cyan
Write-Host ""

# Show operation coverage
Write-Host "Operation Test Coverage:" -ForegroundColor Cyan
foreach ($op in $operationMap.Keys | Sort-Object) {
    $count = $operationMap[$op].Tests.Count
    Write-Host "  $op`: $count tests" -ForegroundColor $(if ($count -gt 5) { "Green" } elseif ($count -gt 2) { "Yellow" } else { "Gray" })
}
Write-Host ""

# Show endpoint coverage
Write-Host "Most Tested Endpoints:" -ForegroundColor Cyan
$endpointMap.GetEnumerator() | 
    Sort-Object { $_.Value.Tests.Count } -Descending | 
    Select-Object -First 10 | 
    ForEach-Object {
        $color = if ($_.Value.Tests.Count -gt 10) { "Green" } elseif ($_.Value.Tests.Count -gt 5) { "Yellow" } else { "Gray" }
        Write-Host "  $($_.Key): $($_.Value.Tests.Count) tests" -ForegroundColor $color
    }
Write-Host ""

# Create output
$output = @{
    Summary = @{
        TotalTests = $testAnalysis.Count
        TotalEndpoints = $endpointMap.Count
        TotalOperations = $operationMap.Count
        AnalysisDate = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    }
    Tests = $testAnalysis
    Endpoints = $endpointMap
    Operations = $operationMap
}

$json = $output | ConvertTo-Json -Depth 10
$json | Out-File -FilePath $OutputPath -Encoding UTF8

Write-Host "✅ Analysis saved to: $OutputPath" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Review the operation coverage in the JSON file" -ForegroundColor White
Write-Host "2. Use the Operations section to update apiTestIds in CapabilityRegistry.json" -ForegroundColor White
Write-Host "3. Run Update-CapabilityRegistry-WithTests.ps1 to merge the data" -ForegroundColor White
