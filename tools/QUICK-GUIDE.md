# Capability Registry Management Guide

Quick reference for updating capability registry with test data and API validation.

## Quick Start

Run the full workflow:
```powershell
cd tools
.\Refresh-CapabilityRegistry.ps1
```

## What the Scripts Do

### 1. **Analyze-ApiTests.ps1** - Extract test information
Scans Python test files to find:
- What API endpoints each test calls (`/api/team`, `/api/courses`, etc.)
- HTTP methods used (GET, POST, PUT, DELETE)
- Which operations are being tested (GetTeamMembers, CreateCourse, etc.)

**Example output:**
```
GetTeamMembers: 15 tests
  test_team.py::test_get_all_members
  test_team.py::test_filter_active_members
  ...
```

### 2. **Update-CapabilityStatus.ps1** - Validate against Swagger
Checks if operations exist in the live API:
- Fetches Swagger/OpenAPI document
- Validates each operation exists
- Updates status: Planned → InProgress → ApiImplemented

### 3. **Update-CapabilityRegistry-WithTests.ps1** - Merge test data
Adds test IDs to registry and updates status based on coverage:
- Adds apiTestIds from analysis
- Updates status based on test count
- Shows coverage: "3/4 operations, 12 total tests"

## Status Progression

```
Planned
  ↓ (operations exist in Swagger)
ApiImplemented
  ↓ (≥3 tests covering all operations)
ApiVerified
  ↓ (GUI tests added)
FullyVerified
```

## Example Workflow

```powershell
# 1. Analyze tests in InternalAI repo
.\Analyze-ApiTests.ps1 -InternalAIPath "C:\git\InternalAI" -Verbose

# Review the analysis
code .\api-test-analysis.json

# 2. Update registry with test data (preview first)
.\Update-CapabilityRegistry-WithTests.ps1 -DryRun

# 3. Apply changes
.\Update-CapabilityRegistry-WithTests.ps1

# 4. Validate against live API
.\Update-CapabilityStatus.ps1 -SwaggerUrl "http://localhost:5000/api/swagger.json"

# 5. Rebuild and deploy
dotnet build ..\src\ApiFirst.LlmOrchestration.McpServer
```

## Or use the all-in-one script:
```powershell
.\Refresh-CapabilityRegistry.ps1 -DryRun  # Preview
.\Refresh-CapabilityRegistry.ps1           # Apply
```
