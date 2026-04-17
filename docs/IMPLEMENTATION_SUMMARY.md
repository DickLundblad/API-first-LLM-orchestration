# API-First Capability Registry - Implementation Summary

## 🎯 What Was Implemented

A complete **API-first capability registry architecture** for tracking, validating, and reporting on system use case capabilities with emphasis on API operations and automated testing as primary evidence.

## 📦 New Components

### 1. Core Models (`src/ApiFirst.LlmOrchestration/Registry/`)

#### `UseCaseCapability` (renamed from `Capability`)
- **Required**: API operations, description, category
- **Primary evidence**: API test IDs
- **Optional**: GUI route, GUI tests, screenshots
- **Evidence level**: Configurable requirement (ApiExecution → Comprehensive)

```csharp
public sealed record UseCaseCapability(
    string Id,
    string Name,
    string Description,
    string Category,
    CapabilityStatus Status,
    IReadOnlyList<string> ApiOperationIds,           // REQUIRED
    IReadOnlyList<string>? ApiTestIds = null,        // PRIMARY EVIDENCE
    string? GuiRoute = null,                          // OPTIONAL
    IReadOnlyList<string>? GuiTestIds = null,        // TERTIARY EVIDENCE
    EvidenceLevel RequiredEvidenceLevel = EvidenceLevel.ApiTests);
```

#### `CapabilityEvidence`
- **Type hierarchy**: ApiAutomatedTest (primary) → IntegrationTest → ApiExecution → GuiAutomatedTest (tertiary) → GuiScreenshot (documentation)
- **Source tracking**: CiCdPipeline, LocalTestRunner, RuntimeValidator, etc.
- **Timestamp and details** for audit trail

```csharp
public sealed record CapabilityEvidence(
    string CapabilityId,
    EvidenceType Type,           // Hierarchical importance
    EvidenceStatus Status,
    EvidenceSource Source,       // Traceability
    DateTime Timestamp,
    string? Details = null,
    IReadOnlyDictionary<string, object>? Data = null);
```

### 2. Capability Registry (`CapabilityRegistry.cs`)

**Features:**
- Register and query capabilities by ID, category, operation, status
- Record evidence with smart `LastVerified` updates (only for meaningful evidence)
- **API test coverage calculation**: % of operations covered by tests
- **Evidence requirement checking**: Does capability meet its configured evidence level?
- Load/save from JSON

**Key Methods:**
```csharp
void RegisterCapability(UseCaseCapability capability)
UseCaseCapability? GetCapability(string id)
IReadOnlyList<UseCaseCapability> GetCapabilitiesByCategory(string category)
void RecordEvidence(CapabilityEvidence evidence)
double GetApiTestCoverage(string capabilityId)
bool MeetsEvidenceRequirement(string capabilityId)
```

### 3. Runtime Capability Validator (`RuntimeCapabilityValidator.cs`)

**Selective validation** to avoid side effects:
- `ValidationScope.SafeOperationsOnly` (default): Only GET/HEAD operations
- `ValidationScope.AllOperations`: All operations (use with caution!)
- `ValidationScope.None`: No validation

**Features:**
- Validates capability by checking if operations exist in Swagger
- Optionally executes safe operations (GET/HEAD only by default)
- Records evidence with proper source (`RuntimeValidator`)
- Returns detailed validation results with `Executed` flag

**Key Methods:**
```csharp
Task<CapabilityValidationResult> ValidateCapabilityAsync(
    string capabilityId,
    string swaggerUrl,
    string apiBaseUrl,
    ValidationScope scope = ValidationScope.SafeOperationsOnly,
    IReadOnlyList<string>? specificOperationIds = null,
    CancellationToken cancellationToken = default)

CapabilityHealthReport GetHealthReport()
```

### 4. MCP Server Integration

**New Tools:**

| Tool | Description | Read-Only |
|------|-------------|-----------|
| `list_capabilities` | List all capabilities with filters (category, status) | Yes |
| `get_capability` | Get detailed info including evidence and test coverage | Yes |
| `validate_capability` | Validate capability at runtime (selective scope) | No |
| `capability_health` | System health report focusing on API metrics | Yes |

**Enhanced Output:**
- API test coverage percentage
- Evidence requirement status (met/not met)
- Separate API tests from GUI tests
- Evidence source tracking

**Example MCP Call:**
```json
{
  "method": "tools/call",
  "params": {
    "name": "get_capability",
    "arguments": {
      "capabilityId": "team-member-management"
    }
  }
}
```

**Response:**
```json
{
  "id": "team-member-management",
  "name": "Team Member Management",
  "apiOperations": ["GetTeamMembers", "UpdateTeamMember"],
  "apiTests": ["TeamTests.GetAll", "TeamTests.Update"],
  "apiTestCoverage": 100.0,
  "hasGui": true,
  "guiRoute": "/team",
  "guiTests": ["TeamUiTests.CanEdit"],
  "requiredEvidenceLevel": "ApiTests",
  "meetsEvidenceRequirement": true,
  "evidence": [
    {
      "type": "ApiAutomatedTest",
      "status": "Success",
      "source": "CiCdPipeline",
      "timestamp": "2025-06-18T10:30:00Z"
    }
  ]
}
```

## 📊 Enums and Status

### `CapabilityStatus`
- `Planned` → Not implemented
- `InProgress` → Development started
- `ApiImplemented` → API exists, no tests yet
- `ApiVerified` → API + automated tests ✅ (production-ready minimum)
- `FullyVerified` → API + GUI tests (if GUI exists)
- `Deprecated` → Marked for removal

### `EvidenceLevel`
- `ApiExecution` → Just check it runs (monitoring)
- `ApiTests` → API automated tests (recommended default)
- `ApiAndGuiTests` → API + GUI tests (if GUI exists)
- `Comprehensive` → Everything + performance benchmarks

### `EvidenceType` (Priority Order)
1. `ApiAutomatedTest` (PRIMARY)
2. `IntegrationTest` (IMPORTANT)
3. `ApiExecution` (SECONDARY)
4. `GuiAutomatedTest` (TERTIARY - only if GUI exists)
5. `PerformanceBenchmark` (OPTIONAL)
6. `GuiScreenshot` (DOCUMENTATION ONLY)
7. `ManualVerification` (FALLBACK)

### `EvidenceSource`
- `CiCdPipeline` (highest trust)
- `LocalTestRunner`
- `RuntimeValidator`
- `AutomatedMonitoring`
- `ManualTesting`
- `External`

### `ValidationScope`
- `None` → Don't validate
- `SafeOperationsOnly` (default) → Only GET/HEAD
- `AllOperations` → Everything (dangerous - side effects!)

## 📁 Files Created/Modified

### New Files
```
src/ApiFirst.LlmOrchestration/Registry/
├── Capability.cs                           # Domain models (NEW)
├── CapabilityRegistry.cs                   # Registry implementation (NEW)
└── RuntimeCapabilityValidator.cs           # Validation logic (NEW)

src/ApiFirst.LlmOrchestration.McpServer/
└── CapabilityRegistry.json                 # Capability definitions (NEW)

tests/ApiFirst.LlmOrchestration.Tests/Examples/
└── CapabilityRegistryTestIntegration.cs    # Test integration examples (NEW)

docs/
├── API_FIRST_PRINCIPLES.md                 # API-first design principles (NEW)
├── CAPABILITY_REGISTRY_ARCHITECTURE.md     # Architecture guide (NEW)
├── QUICKSTART.md                           # Getting started guide (NEW)
├── ARCHITECTURE_DIAGRAMS.md                # Visual diagrams (NEW)
└── IMPLEMENTATION_CHECKLIST.md             # Implementation roadmap (NEW)
```

### Modified Files
```
src/ApiFirst.LlmOrchestration.McpServer/
└── McpServer.cs                            # Added capability tools (MODIFIED)
```

## 🏗️ Architecture Principles

### 1. API-First
- **API operations are core** (required)
- **API tests are primary evidence** (critical)
- **GUI is optional** (tertiary evidence if exists)
- Capabilities without GUI are perfectly valid

### 2. Prepared Index (Not Discovery)
- Registry is **manually curated** declaration of capabilities
- Not auto-generated from Swagger
- Requires human judgment to group operations into use cases
- Declarative documentation of what system SHOULD do

### 3. Selective Runtime Validation
- **Default: Safe operations only** (GET/HEAD)
- Prevents side effects during validation
- Explicit opt-in for unsafe operations
- `Executed` flag shows what actually ran

### 4. Evidence-Based
- All claims backed by evidence
- Evidence has **type hierarchy** (API tests > GUI tests)
- Evidence has **source** (CI/CD > local)
- Evidence has **timestamp** for audit trail

### 5. Capability Maturity Model
- `Planned` → `InProgress` → `ApiImplemented` → **`ApiVerified`** (minimum for production) → `FullyVerified`
- GUI testing only required if GUI exists
- Backend-only capabilities are first-class citizens

## 📈 Health Metrics (API-First)

### Primary Metrics
1. **API Test Coverage** (most important)
   - % of operations with automated tests
   - Average coverage across all capabilities

2. **Evidence Quality Score**
   - Weighted by evidence type and source
   - CiCdPipeline evidence > LocalTestRunner

3. **Capability Maturity**
   - % at each status level
   - Focus on `ApiVerified` as minimum bar

### Health Report Example
```json
{
  "totalCapabilities": 10,
  "apiTestedCapabilities": 8,              // 80% have API tests ✅
  "averageApiTestCoverage": 85.5,          // Avg 85.5% coverage ✅
  "verifiedCapabilities": 7,               // 70% runtime-verified
  "guiTestedCapabilities": 4,              // 40% have GUI tests (OK!)
  "recommendation": "Healthy - excellent API test coverage"
}
```

## 🧪 Test Integration

### Recording API Test Evidence
```csharp
[Fact]
public void GetTeamMembers_ReturnsAll()
{
    try
    {
        // ... test logic ...

        _registry.RecordEvidence(new CapabilityEvidence(
            "team-member-management",
            EvidenceType.ApiAutomatedTest,      // PRIMARY
            EvidenceStatus.Success,
            EvidenceSource.CiCdPipeline,        // High trust
            DateTime.UtcNow,
            "API: GetTeamMembers returns valid schema"));
    }
    catch (Exception ex)
    {
        _registry.RecordEvidence(new CapabilityEvidence(
            "team-member-management",
            EvidenceType.ApiAutomatedTest,
            EvidenceStatus.Failed,
            EvidenceSource.CiCdPipeline,
            DateTime.UtcNow,
            $"API: Test failed - {ex.Message}"));
        throw;
    }
}
```

### Recording GUI Test Evidence (Optional)
```csharp
[Fact]
public void TeamMemberGui_CanEdit()
{
    // ... GUI test logic ...

    _registry.RecordEvidence(new CapabilityEvidence(
        "team-member-management",
        EvidenceType.GuiAutomatedTest,          // TERTIARY
        EvidenceStatus.Success,
        EvidenceSource.LocalTestRunner,
        DateTime.UtcNow,
        "GUI: Can edit team member profile"));
}
```

## ✅ Best Practices

### DO
1. ✅ Write API tests first (before GUI tests)
2. ✅ Default to `ApiTests` evidence level for most capabilities
3. ✅ Use `SafeOperationsOnly` for runtime validation
4. ✅ Track evidence source (CiCdPipeline > LocalTestRunner)
5. ✅ Accept capabilities without GUI (`guiRoute: null` is valid)
6. ✅ Curate registry manually (don't auto-generate)
7. ✅ Prioritize integration tests for cross-system flows

### DON'T
1. ❌ Don't require GUI for all capabilities
2. ❌ Don't use GUI tests as primary evidence
3. ❌ Don't run unsafe operations in runtime validation
4. ❌ Don't confuse GuiScreenshot with verification (it's just documentation)
5. ❌ Don't treat registry as discovery (it's declaration)
6. ❌ Don't skip API tests thinking GUI tests are enough

## 🚀 Next Steps

1. **Populate `CapabilityRegistry.json`** with your use cases
2. **Integrate with test suite** to record evidence automatically
3. **Run `capability_health`** to see current state
4. **Validate capabilities** with `SafeOperationsOnly` scope
5. **Set up CI/CD integration** to record evidence from pipeline

## 📚 Documentation

- **[API_FIRST_PRINCIPLES.md](API_FIRST_PRINCIPLES.md)** - Core design principles
- **[QUICKSTART.md](QUICKSTART.md)** - Getting started guide
- **[ARCHITECTURE_DIAGRAMS.md](ARCHITECTURE_DIAGRAMS.md)** - Visual diagrams
- **[CAPABILITY_REGISTRY_ARCHITECTURE.md](CAPABILITY_REGISTRY_ARCHITECTURE.md)** - Full architecture
- **[IMPLEMENTATION_CHECKLIST.md](IMPLEMENTATION_CHECKLIST.md)** - Roadmap

---

**Built with ❤️ for API-first LLM orchestration**
