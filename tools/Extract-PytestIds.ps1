# Extract-PytestIds.ps1
# Extract pytest test IDs from Python test files in the InternalAI repository

param(
    [string]$InternalAIPath = "C:\git\InternalAI",
    [string]$OutputPath = ".\pytest-test-ids.json",
    [switch]$GroupByOperation
)

Write-Host "Scanning Python test files in $InternalAIPath..." -ForegroundColor Cyan

if (-not (Test-Path $InternalAIPath)) {
    Write-Error "InternalAI repository not found at: $InternalAIPath"
    exit 1
}

# Find all test files (test_*.py or *_test.py)
$testFiles = Get-ChildItem -Path $InternalAIPath -Filter "test_*.py" -Recurse | 
    Where-Object { $_.FullName -notlike "*\.venv\*" -and $_.FullName -notlike "*\venv\*" }

if ($testFiles.Count -eq 0) {
    Write-Warning "No Python test files found in $InternalAIPath"
    exit 0
}

$allTests = @()
$operationMap = @{}

foreach ($file in $testFiles) {
    Write-Host "`nProcessing: $($file.Name)" -ForegroundColor Yellow

    $content = Get-Content $file.FullName -Raw
    $fileName = $file.Name

    # Extract test function names (def test_*():)
    $testFunctions = [regex]::Matches($content, 'def\s+(test_\w+)\s*\(')

    foreach ($match in $testFunctions) {
        $testName = $match.Groups[1].Value
        $testId = "${fileName}::${testName}"

        $allTests += $testId

        # Try to infer which API operation this tests
        $inferredOps = @()

        # Map common test patterns to operations
        if ($testName -match 'team|member' -and $testName -notmatch 'delete|update') {
            $inferredOps += "GetTeamMembers"
        }
        if ($testName -match 'team.*member' -and $testName -match 'delete') {
            $inferredOps += "DeleteTeamMember"
        }
        if ($testName -match 'team.*member' -and $testName -match 'update|edit') {
            $inferredOps += "UpdateTeamMember"
        }
        if ($testName -match 'get.*team.*member|team_member.*get' -and $testName -notmatch 'all|list') {
            $inferredOps += "GetTeamMember"
        }
        if ($testName -match 'course' -and $testName -notmatch 'delete|update|create|enroll') {
            $inferredOps += "GetCourses"
        }
        if ($testName -match 'course.*create') {
            $inferredOps += "CreateCourse"
        }
        if ($testName -match 'course.*update') {
            $inferredOps += "UpdateCourse"
        }
        if ($testName -match 'course.*approve') {
            $inferredOps += "ApproveCourse"
        }
        if ($testName -match 'course.*archive') {
            $inferredOps += "ArchiveCourse"
        }
        if ($testName -match 'enroll') {
            $inferredOps += "EnrollCourse"
        }
        if ($testName -match 'consultant.*course|member.*course' -and $testName -notmatch 'enroll') {
            $inferredOps += "GetConsultantCourses"
        }
        if ($testName -match 'login') {
            $inferredOps += "Login"
        }
        if ($testName -match 'forgot.*password') {
            $inferredOps += "ForgotPassword"
        }
        if ($testName -match 'reset.*password') {
            $inferredOps += "ResetPassword"
        }
        if ($testName -match 'verify.*email') {
            $inferredOps += "VerifyEmail"
        }

        foreach ($op in $inferredOps) {
            if (-not $operationMap.ContainsKey($op)) {
                $operationMap[$op] = @()
            }
            if ($testId -notin $operationMap[$op]) {
                $operationMap[$op] += $testId
            }
        }

        Write-Host "  ✓ $testId" -ForegroundColor Gray
        if ($inferredOps.Count -gt 0) {
            Write-Host "    → $($inferredOps -join ', ')" -ForegroundColor DarkGray
        }
    }
}

Write-Host "`n" -NoNewline
Write-Host "=" * 80 -ForegroundColor Cyan
Write-Host "Found $($allTests.Count) pytest tests" -ForegroundColor Green

if ($GroupByOperation) {
    Write-Host "Grouped by $($operationMap.Keys.Count) API operations" -ForegroundColor Green
}

Write-Host "=" * 80 -ForegroundColor Cyan

# Output results
if ($GroupByOperation) {
    $output = $operationMap
    Write-Host "`nTests grouped by API operation:" -ForegroundColor Cyan
    foreach ($op in $operationMap.Keys | Sort-Object) {
        Write-Host "  $op`: $($operationMap[$op].Count) tests" -ForegroundColor Yellow
    }
}
else {
    $output = $allTests
}

$json = $output | ConvertTo-Json -Depth 10
$json | Out-File -FilePath $OutputPath -Encoding UTF8

Write-Host "`n✅ Saved to: $OutputPath" -ForegroundColor Green
Write-Host "`nNext steps:" -ForegroundColor Cyan
Write-Host "1. Review the mapping in $OutputPath" -ForegroundColor White
Write-Host "2. Use Update-CapabilityRegistry.ps1 to merge these test IDs into your capability registry" -ForegroundColor White
