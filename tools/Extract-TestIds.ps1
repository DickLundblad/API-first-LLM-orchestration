# Extract-TestIds.ps1
# Automatically extract test IDs from your test files for use in CapabilityRegistry.json

param(
    [string]$TestProjectPath = "tests\ApiFirst.LlmOrchestration.Tests",
    [string]$OutputFormat = "json",  # json, csv, or list
    [switch]$GroupByClass,
    [switch]$InferCapabilities
)

Write-Host "Scanning test files in $TestProjectPath..." -ForegroundColor Cyan

# Find all test files
$testFiles = Get-ChildItem -Path $TestProjectPath -Filter "*Tests.cs" -Recurse

$allTests = @()

foreach ($file in $testFiles) {
    Write-Host "`nProcessing: $($file.Name)" -ForegroundColor Yellow

    $content = Get-Content $file.FullName -Raw

    # Extract namespace
    $namespace = if ($content -match 'namespace\s+([\w\.]+)') { $matches[1] } else { "" }

    # Extract class name
    $className = if ($content -match 'public\s+(?:sealed\s+)?class\s+(\w+)') { $matches[1] } else { "" }

    # Find all test methods (NUnit: [Test], xUnit: [Fact]/[Theory])
    $testMethods = [regex]::Matches($content, '\[(Test|Fact|Theory)\]\s*(?:\[[\w\s,=\(\)"]+\]\s*)*public\s+(?:async\s+)?(?:Task|void)\s+(\w+)')

    foreach ($match in $testMethods) {
        $methodName = $match.Groups[2].Value
        $testId = "$className.$methodName"

        # Try to infer capability from test/method name
        $inferredCapability = $null
        if ($InferCapabilities) {
            if ($className -match 'Team') { $inferredCapability = "team-member-management" }
            elseif ($className -match 'Course') { $inferredCapability = "course-management" }
            elseif ($className -match 'Enroll') { $inferredCapability = "course-enrollment" }
            elseif ($className -match 'Auth|Login') { $inferredCapability = "authentication" }
            elseif ($className -match 'Swagger|Document') { $inferredCapability = "api-metadata" }
        }

        $testInfo = [PSCustomObject]@{
            TestId = $testId
            ClassName = $className
            MethodName = $methodName
            File = $file.Name
            FullPath = $file.FullName
            InferredCapability = $inferredCapability
        }

        $allTests += $testInfo
        Write-Host "  ✓ $testId" -ForegroundColor Gray
    }
}

Write-Host "`n" -NoNewline
Write-Host "=" * 80 -ForegroundColor Cyan
Write-Host "Found $($allTests.Count) tests" -ForegroundColor Green
Write-Host "=" * 80 -ForegroundColor Cyan

# Output based on format
switch ($OutputFormat) {
    "json" {
        if ($GroupByClass) {
            $grouped = $allTests | Group-Object -Property ClassName
            $output = @{}
            foreach ($group in $grouped) {
                $output[$group.Name] = $group.Group.TestId
            }
            $json = $output | ConvertTo-Json -Depth 10
            Write-Host "`nJSON Output (grouped by class):"
            Write-Host $json
        }
        elseif ($InferCapabilities) {
            $grouped = $allTests | Where-Object { $_.InferredCapability } | Group-Object -Property InferredCapability
            $output = @{}
            foreach ($group in $grouped) {
                $output[$group.Name] = $group.Group.TestId
            }
            $json = $output | ConvertTo-Json -Depth 10
            Write-Host "`nJSON Output (grouped by inferred capability):"
            Write-Host $json
        }
        else {
            $json = $allTests.TestId | ConvertTo-Json
            Write-Host "`nJSON Output (all test IDs):"
            Write-Host $json
        }
    }

    "csv" {
        Write-Host "`nCSV Output:"
        $allTests | Export-Csv -Path "test-ids.csv" -NoTypeInformation
        Write-Host "Saved to test-ids.csv"
        $allTests | Format-Table -AutoSize
    }

    "list" {
        Write-Host "`nTest IDs (one per line):"
        $allTests.TestId | ForEach-Object { Write-Host "  $_" }
    }
}

# If InferCapabilities, show suggestions for CapabilityRegistry.json
if ($InferCapabilities) {
    Write-Host "`n" -NoNewline
    Write-Host "=" * 80 -ForegroundColor Cyan
    Write-Host "Suggested capability updates:" -ForegroundColor Green
    Write-Host "=" * 80 -ForegroundColor Cyan

    $grouped = $allTests | Where-Object { $_.InferredCapability } | Group-Object -Property InferredCapability

    foreach ($group in $grouped) {
        Write-Host "`n📋 Capability: $($group.Name)" -ForegroundColor Yellow
        Write-Host "   Add these to 'apiTestIds' in CapabilityRegistry.json:" -ForegroundColor Gray
        Write-Host '   "apiTestIds": [' -ForegroundColor White
        $testIds = $group.Group.TestId
        for ($i = 0; $i -lt $testIds.Count; $i++) {
            $comma = if ($i -lt $testIds.Count - 1) { "," } else { "" }
            Write-Host "     `"$($testIds[$i])`"$comma" -ForegroundColor White
        }
        Write-Host '   ]' -ForegroundColor White
    }
}

# Summary by class
Write-Host "`n" -NoNewline
Write-Host "=" * 80 -ForegroundColor Cyan
Write-Host "Tests by class:" -ForegroundColor Green
Write-Host "=" * 80 -ForegroundColor Cyan

$allTests | Group-Object -Property ClassName | Sort-Object Name | ForEach-Object {
    Write-Host "`n$($_.Name) ($($_.Count) tests):" -ForegroundColor Yellow
    $_.Group | ForEach-Object {
        Write-Host "  - $($_.MethodName)" -ForegroundColor Gray
    }
}

Write-Host "`n" -NoNewline
Write-Host "=" * 80 -ForegroundColor Cyan
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "=" * 80 -ForegroundColor Cyan
Write-Host "1. Copy test IDs to CapabilityRegistry.json" -ForegroundColor White
Write-Host "2. Or use: .\Extract-TestIds.ps1 -InferCapabilities -OutputFormat json" -ForegroundColor White
Write-Host "3. Update capability status to 'ApiVerified' if tests pass" -ForegroundColor White
Write-Host "`n"
