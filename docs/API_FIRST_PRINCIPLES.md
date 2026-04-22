# API-First Principles for Capability Registry

## 🎯 Core Principles

### 1. **API is Core, GUI is Optional**

```
┌─────────────────────────────────────┐
│   Use Case Capability               │
├─────────────────────────────────────┤
│  ✅ REQUIRED                         │
│  • API Operations (must have)       │
│  • API Tests (critical evidence)    │
│                                     │
│  ⚪ OPTIONAL                         │
│  • GUI Route (if UI exists)         │
│  • GUI Tests (tertiary evidence)    │
│  • Screenshots (documentation)      │
└─────────────────────────────────────┘
```

**Example:**
```json
{
  "id": "advanced-search",
  "name": "Advanced Search & Filtering",
  "apiOperationIds": ["SearchTeamMembers", "SearchCourses"],  // ✅ CORE
  "apiTestIds": ["SearchTests.AdvancedFilters_Work"],         // ✅ PRIMARY EVIDENCE
  "guiRoute": null,                                            // ⚪ No GUI yet - OK!
  "status": "ApiVerified"                                      // ✅ Still verified
}
```

---

### 2. **Evidence Hierarchy (API-first)**

```
┌──────────────────────────────────────────┐
│  Evidence Priority (High → Low)          │
├──────────────────────────────────────────┤
│  1️⃣ ApiAutomatedTest     (PRIMARY)       │
│     • Proves API contract works          │
│     • Repeatable, automated              │
│     • Required for "ApiVerified" status  │
│                                          │
│  2️⃣ IntegrationTest      (IMPORTANT)     │
│     • Proves end-to-end flow             │
│     • Cross-system validation            │
│                                          │
│  3️⃣ ApiExecution         (SECONDARY)     │
│     • Runtime check (right now)          │
│     • Good for monitoring                │
│     • Not permanent proof                │
│                                          │
│  4️⃣ GuiAutomatedTest     (TERTIARY)      │
│     • Only if GUI exists                 │
│     • Supplements API tests              │
│     • Never replaces API tests           │
│                                          │
│  5️⃣ PerformanceBenchmark (OPTIONAL)      │
│     • For critical paths                 │
│     • Comprehensive level only           │
│                                          │
│  6️⃣ GuiScreenshot        (DOCUMENTATION) │
│     • Not verification                   │
│     • Just visual proof of existence     │
│                                          │
│  7️⃣ ManualVerification   (FALLBACK)      │
│     • Use sparingly                      │
│     • Not scalable                       │
└──────────────────────────────────────────┘
```

**Code Example:**
```csharp
// ✅ GOOD: API test as primary evidence
_registry.RecordEvidence(new CapabilityEvidence(
    "team-member-management",
    EvidenceType.ApiAutomatedTest,     // Primary!
    EvidenceStatus.Success,
    EvidenceSource.CiCdPipeline,       // From CI = high trust
    DateTime.UtcNow,
    "API: GetTeamMembers returns valid schema"));

// ⚪ OPTIONAL: GUI test as supplementary evidence
_registry.RecordEvidence(new CapabilityEvidence(
    "team-member-management",
    EvidenceType.GuiAutomatedTest,     // Tertiary
    EvidenceStatus.Success,
    EvidenceSource.LocalTestRunner,
    DateTime.UtcNow,
    "GUI: Can view team members in table"));
```

---

### 3. **Capability Status Workflow (API-first)**

```
Planned
  ↓
InProgress
  ↓
ApiImplemented  ← API exists, no tests yet
  ↓
ApiVerified  ← API + automated tests ✅ (MINIMUM VIABLE)
  ↓
FullyVerified  ← API + tests + (GUI tests if GUI exists) ✅
```

**Key Points:**
- **`ApiVerified`** is the minimum acceptable for production
- **`FullyVerified`** only required if capability has GUI
- Capabilities **without GUI** never become `FullyVerified` (and don't need to!)

---

### 4. **Required Evidence Levels**

```csharp
public enum EvidenceLevel
{
    ApiExecution,      // Just check it runs (monitoring/health checks)
    ApiTests,          // API automated tests (RECOMMENDED DEFAULT)
    ApiAndGuiTests,    // API + GUI tests (if GUI exists)
    Comprehensive      // Everything + performance benchmarks
}
```

**Best Practices:**

| Capability Type | Recommended Level | Rationale |
|---|---|---|
| Backend-only API | `ApiTests` | No GUI to test |
| API with simple GUI | `ApiTests` | GUI follows API, test API |
| Critical user-facing feature | `ApiAndGuiTests` | User experience matters |
| High-traffic endpoint | `Comprehensive` | Performance critical |

**Example:**
```json
{
  "id": "authentication",
  "requiredEvidenceLevel": "Comprehensive",  // Critical path
  "apiTestIds": ["AuthTests.Login_Works"],
  "status": "ApiVerified"  // Not fully verified until perf benchmarked
}
```

---

### 5. **Runtime Validation is Selective**

**Problem:** Running all API operations → side effects (DELETE, POST, etc.)

**Solution:** Validation scopes

```csharp
public enum ValidationScope
{
    None,                  // Don't validate
    SafeOperationsOnly,    // Only GET/HEAD (DEFAULT) ✅
    AllOperations          // Everything (dangerous!) ⚠️
}
```

**MCP Tool Usage:**
```json
// ✅ SAFE: Only validates GET operations
{
  "method": "tools/call",
  "params": {
    "name": "validate_capability",
    "arguments": {
      "capabilityId": "team-member-management",
      "scope": "SafeOperationsOnly"
    }
  }
}

// ⚠️ DANGEROUS: Will execute DELETE, POST, etc.
{
  "method": "tools/call",
  "params": {
    "name": "validate_capability",
    "arguments": {
      "capabilityId": "team-member-management",
      "scope": "AllOperations"  // Use with caution!
    }
  }
}
```

**Implementation:**
```csharp
var isSafe = operation.Method.Equals("GET") || operation.Method.Equals("HEAD");

if (scope == ValidationScope.SafeOperationsOnly && !isSafe)
{
    // Skip execution, just verify it exists in Swagger
    results.Add(new OperationValidationResult(
        operationId,
        true,
        "Operation exists but not executed (unsafe)",
        null,
        null,
        false)); // Executed = false
}
```

---

### 6. **Capability Registry = Prepared Index (Not Discovery)**

**❌ NOT discovery:**
```csharp
// WRONG: Try to auto-discover capabilities from Swagger
foreach (var operation in swagger.Operations)
{
    var capability = new UseCaseCapability(...); // Invented!
}
```

**✅ Prepared index:**
```json
// RIGHT: Manually curated, declared capabilities
[
  {
    "id": "team-member-management",
    "name": "Team Member Management",
    "apiOperationIds": ["GetTeamMembers", "UpdateTeamMember"],
    "apiTestIds": ["TeamTests.GetAll", "TeamTests.Update"]
  }
]
```

**Rationale:**
- Capabilities are **business use cases**, not just API endpoints
- Requires **human judgment**: which operations belong together?
- Registry is **declarative** documentation of what the system SHOULD do

---

### 7. **GUI Support → Test Coverage (Not Just Routes)**

**❌ Old approach:**
```json
{
  "guiRoute": "/team",
  "screenshotUrl": "team-list.png"  // Just visual proof
}
```

**✅ API-first approach:**
```json
{
  "guiRoute": "/team",
  "guiTestIds": [
    "TeamUiTests.CanViewList",
    "TeamUiTests.CanEditMember"
  ],  // Automated proof!
  "apiTestIds": [
    "TeamApiTests.GetAll",
    "TeamApiTests.Update"
  ]  // Primary proof!
}
```

**Relationship:**
```
API Operations
  ↓ (drives)
API Tests ← PRIMARY EVIDENCE
  ↓ (if GUI exists)
GUI Features
  ↓ (verified by)
GUI Tests ← TERTIARY EVIDENCE
  ↓ (documented by)
Screenshots ← DOCUMENTATION ONLY
```

---

## 📊 Health Metrics (API-first)

### Primary Metrics

1. **API Test Coverage** (most important)
   ```
   Coverage = (Operations with tests / Total operations) × 100%
   ```

2. **Evidence Quality**
   ```
   Quality Score:
   - ApiAutomatedTest from CiCdPipeline: 10 points
   - IntegrationTest from CiCdPipeline: 9 points
   - ApiAutomatedTest from LocalTestRunner: 7 points
   - ApiExecution from RuntimeValidator: 5 points
   - GuiAutomatedTest: 3 points
   - GuiScreenshot: 1 point
   - ManualVerification: 1 point
   ```

3. **Capability Maturity**
   ```
   Maturity Levels:
   - Planned: 0%
   - InProgress: 25%
   - ApiImplemented: 50%
   - ApiVerified: 80% ✅ (Production-ready)
   - FullyVerified: 100%
   ```

### Health Report Example

```json
{
  "totalCapabilities": 10,
  "apiTestedCapabilities": 8,          // 80% have API tests ✅
  "averageApiTestCoverage": 85.5,      // Avg 85.5% operation coverage ✅
  "verifiedCapabilities": 7,           // 70% runtime-verified
  "guiTestedCapabilities": 4,          // Only 40% have GUI tests (OK!)

  "recommendation": "System is healthy - API test coverage is excellent. GUI testing is optional for backend-focused capabilities."
}
```

---

## 🏆 Best Practices Summary

### ✅ DO

1. **Write API tests first** (before GUI tests)
2. **Default to `ApiTests` evidence level** for most capabilities
3. **Use `SafeOperationsOnly` for runtime validation**
4. **Track evidence source** (`CiCdPipeline` > `LocalTestRunner`)
5. **Accept capabilities without GUI** (`guiRoute: null` is OK!)
6. **Curate registry manually** (don't auto-generate)
7. **Prioritize integration tests** for cross-system flows

### ❌ DON'T

1. **Don't require GUI for all capabilities**
2. **Don't use GUI tests as primary evidence**
3. **Don't run unsafe operations in runtime validation**
4. **Don't confuse evidence types:**
   - `GuiScreenshot` ≠ verification (just documentation)
   - `ManualVerification` ≠ scalable (use sparingly)
5. **Don't treat registry as discovery** (it's declaration)
6. **Don't skip API tests** thinking GUI tests are enough

---

## 📚 Example: Well-Designed Capability (API-first)

```json
{
  "id": "course-management",
  "name": "Course Management",
  "description": "Full lifecycle management of courses via API",
  "category": "Courses",
  "status": "ApiVerified",

  // ✅ CORE: API operations
  "apiOperationIds": [
    "GetCourses",
    "CreateCourse",
    "UpdateCourse",
    "ApproveCourse",
    "ArchiveCourse"
  ],

  // ✅ PRIMARY EVIDENCE: API automated tests
  "apiTestIds": [
    "CourseTests.GetAll_ReturnsValidSchema",
    "CourseTests.Create_ValidatesInput",
    "CourseTests.Update_UpdatesSuccessfully",
    "CourseTests.Approve_RequiresManagerRole",
    "CourseTests.Archive_SoftDeletes"
  ],

  // ⚪ OPTIONAL: GUI (happens to exist)
  "guiRoute": "/courses",
  "guiFeature": "Courses Management Page",

  // ⚪ TERTIARY EVIDENCE: GUI tests (optional)
  "guiTestIds": [
    "CourseUiTests.CanCreateCourse",
    "CourseUiTests.CanApproveCourse"
  ],

  // Evidence level: API tests sufficient
  "requiredEvidenceLevel": "ApiTests",

  // Metadata
  "metadata": {
    "owner": "LearningSquad",
    "priority": "High",
    "apiVersion": "v1",
    "apiDocumentation": "https://api.internal/docs/courses"
  }
}
```

**Evidence Recorded:**
```
ApiAutomatedTest (5x) ← PRIMARY ✅
IntegrationTest (1x)  ← IMPORTANT ✅
GuiAutomatedTest (2x) ← TERTIARY (nice to have)
ApiExecution (5x)     ← Runtime monitoring

Status: ApiVerified ✅ (production-ready!)
```

---

**Summary:** API-first means that **API operations and API tests** are the core. GUI, screenshots, and manual verification are secondary or for documentation purposes. Registry is a **prepared declaration**, not auto-discovery. Runtime validation is **selective** to avoid side effects.

```
┌─────────────────────────────────────┐
│   Use Case Capability               │
├─────────────────────────────────────┤
│  ✅ REQUIRED                         │
│  • API Operations (must have)       │
│  • API Tests (critical evidence)    │
│                                     │
│  ⚪ OPTIONAL                         │
│  • GUI Route (if UI exists)         │
│  • GUI Tests (tertiary evidence)    │
│  • Screenshots (documentation)      │
└─────────────────────────────────────┘
```

**Exempel:**
```json
{
  "id": "advanced-search",
  "name": "Advanced Search & Filtering",
  "apiOperationIds": ["SearchTeamMembers", "SearchCourses"],  // ✅ CORE
  "apiTestIds": ["SearchTests.AdvancedFilters_Work"],         // ✅ PRIMARY EVIDENCE
  "guiRoute": null,                                            // ⚪ No GUI yet - OK!
  "status": "ApiVerified"                                      // ✅ Still verified
}
```

---

### 2. **Evidenshierarki (API-first)**

```
┌──────────────────────────────────────────┐
│  Evidence Priority (High → Low)          │
├──────────────────────────────────────────┤
│  1️⃣ ApiAutomatedTest     (PRIMARY)       │
│     • Proves API contract works          │
│     • Repeatable, automated              │
│     • Required for "ApiVerified" status  │
│                                          │
│  2️⃣ IntegrationTest      (IMPORTANT)     │
│     • Proves end-to-end flow             │
│     • Cross-system validation            │
│                                          │
│  3️⃣ ApiExecution         (SECONDARY)     │
│     • Runtime check (right now)          │
│     • Good for monitoring                │
│     • Not permanent proof                │
│                                          │
│  4️⃣ GuiAutomatedTest     (TERTIARY)      │
│     • Only if GUI exists                 │
│     • Supplements API tests              │
│     • Never replaces API tests           │
│                                          │
│  5️⃣ PerformanceBenchmark (OPTIONAL)      │
│     • For critical paths                 │
│     • Comprehensive level only           │
│                                          │
│  6️⃣ GuiScreenshot        (DOCUMENTATION) │
│     • Not verification                   │
│     • Just visual proof of existence     │
│                                          │
│  7️⃣ ManualVerification   (FALLBACK)      │
│     • Use sparingly                      │
│     • Not scalable                       │
└──────────────────────────────────────────┘
```

**Kod exempel:**
```csharp
// ✅ GOOD: API test as primary evidence
_registry.RecordEvidence(new CapabilityEvidence(
    "team-member-management",
    EvidenceType.ApiAutomatedTest,     // Primary!
    EvidenceStatus.Success,
    EvidenceSource.CiCdPipeline,       // From CI = high trust
    DateTime.UtcNow,
    "API: GetTeamMembers returns valid schema"));

// ⚪ OPTIONAL: GUI test as supplementary evidence
_registry.RecordEvidence(new CapabilityEvidence(
    "team-member-management",
    EvidenceType.GuiAutomatedTest,     // Tertiary
    EvidenceStatus.Success,
    EvidenceSource.LocalTestRunner,
    DateTime.UtcNow,
    "GUI: Can view team members in table"));
```

---

### 3. **Capability Status Workflow (API-first)**

```
Planned
  ↓
InProgress
  ↓
ApiImplemented  ← API exists, no tests yet
  ↓
ApiVerified  ← API + automated tests ✅ (MINIMUM VIABLE)
  ↓
FullyVerified  ← API + tests + (GUI tests if GUI exists) ✅
```

**Viktiga poänger:**
- **`ApiVerified`** är det minsta acceptabla för production
- **`FullyVerified`** krävs bara om capabilities har GUI
- Capabilities **utan GUI** blir aldrig `FullyVerified` (och behöver inte heller!)

---

### 4. **Required Evidence Levels**

```csharp
public enum EvidenceLevel
{
    ApiExecution,      // Just check it runs (monitoring/health checks)
    ApiTests,          // API automated tests (RECOMMENDED DEFAULT)
    ApiAndGuiTests,    // API + GUI tests (if GUI exists)
    Comprehensive      // Everything + performance benchmarks
}
```

**Best practices:**

| Capability Type | Recommended Level | Rationale |
|---|---|---|
| Backend-only API | `ApiTests` | No GUI to test |
| API with simple GUI | `ApiTests` | GUI follows API, test API |
| Critical user-facing feature | `ApiAndGuiTests` | User experience matters |
| High-traffic endpoint | `Comprehensive` | Performance critical |

**Exempel:**
```json
{
  "id": "authentication",
  "requiredEvidenceLevel": "Comprehensive",  // Critical path
  "apiTestIds": ["AuthTests.Login_Works"],
  "status": "ApiVerified"  // Not fully verified until perf benchmarked
}
```

---

### 5. **Runtime Validation är Selektiv**

**Problem:** Kör alla API operations → side effects (DELETE, POST, etc.)

**Lösning:** Validation scopes

```csharp
public enum ValidationScope
{
    None,                  // Don't validate
    SafeOperationsOnly,    // Only GET/HEAD (DEFAULT) ✅
    AllOperations          // Everything (dangerous!) ⚠️
}
```

**MCP Tool Usage:**
```json
// ✅ SAFE: Only validates GET operations
{
  "method": "tools/call",
  "params": {
    "name": "validate_capability",
    "arguments": {
      "capabilityId": "team-member-management",
      "scope": "SafeOperationsOnly"
    }
  }
}

// ⚠️ DANGEROUS: Will execute DELETE, POST, etc.
{
  "method": "tools/call",
  "params": {
    "name": "validate_capability",
    "arguments": {
      "capabilityId": "team-member-management",
      "scope": "AllOperations"  // Use with caution!
    }
  }
}
```

**Implementering:**
```csharp
var isSafe = operation.Method.Equals("GET") || operation.Method.Equals("HEAD");

if (scope == ValidationScope.SafeOperationsOnly && !isSafe)
{
    // Skip execution, just verify it exists in Swagger
    results.Add(new OperationValidationResult(
        operationId,
        true,
        "Operation exists but not executed (unsafe)",
        null,
        null,
        false)); // Executed = false
}
```

---

### 6. **Capability Registry = Prepared Index (inte discovery)**

**❌ INTE discovery:**
```csharp
// WRONG: Try to auto-discover capabilities from Swagger
foreach (var operation in swagger.Operations)
{
    var capability = new UseCaseCapability(...); // Invented!
}
```

**✅ Prepared index:**
```json
// RIGHT: Manually curated, declared capabilities
[
  {
    "id": "team-member-management",
    "name": "Team Member Management",
    "apiOperationIds": ["GetTeamMembers", "UpdateTeamMember"],
    "apiTestIds": ["TeamTests.GetAll", "TeamTests.Update"]
  }
]
```

**Rationale:**
- Capabilities är **business use cases**, inte bara API endpoints
- Kräver **mänskligt omdöme**: vilka operations hör ihop?
- Registry är **deklarativ** dokumentation av vad systemet SKA kunna

---

### 7. **GUI Support → Test Coverage (inte bara routes)**

**❌ Gammal approach:**
```json
{
  "guiRoute": "/team",
  "screenshotUrl": "team-list.png"  // Just visual proof
}
```

**✅ API-first approach:**
```json
{
  "guiRoute": "/team",
  "guiTestIds": [
    "TeamUiTests.CanViewList",
    "TeamUiTests.CanEditMember"
  ],  // Automated proof!
  "apiTestIds": [
    "TeamApiTests.GetAll",
    "TeamApiTests.Update"
  ]  // Primary proof!
}
```

**Relationship:**
```
API Operations
  ↓ (drives)
API Tests ← PRIMARY EVIDENCE
  ↓ (if GUI exists)
GUI Features
  ↓ (verified by)
GUI Tests ← TERTIARY EVIDENCE
  ↓ (documented by)
Screenshots ← DOCUMENTATION ONLY
```

---

## 📊 Health Metrics (API-first)

### Primary Metrics

1. **API Test Coverage** (most important)
   ```
   Coverage = (Operations with tests / Total operations) × 100%
   ```

2. **Evidence Quality**
   ```
   Quality Score:
   - ApiAutomatedTest from CiCdPipeline: 10 points
   - IntegrationTest from CiCdPipeline: 9 points
   - ApiAutomatedTest from LocalTestRunner: 7 points
   - ApiExecution from RuntimeValidator: 5 points
   - GuiAutomatedTest: 3 points
   - GuiScreenshot: 1 point
   - ManualVerification: 1 point
   ```

3. **Capability Maturity**
   ```
   Maturity Levels:
   - Planned: 0%
   - InProgress: 25%
   - ApiImplemented: 50%
   - ApiVerified: 80% ✅ (Production-ready)
   - FullyVerified: 100%
   ```

### Health Report Example

```json
{
  "totalCapabilities": 10,
  "apiTestedCapabilities": 8,          // 80% have API tests ✅
  "averageApiTestCoverage": 85.5,      // Avg 85.5% operation coverage ✅
  "verifiedCapabilities": 7,           // 70% runtime-verified
  "guiTestedCapabilities": 4,          // Only 40% have GUI tests (OK!)

  "recommendation": "System is healthy - API test coverage is excellent. GUI testing is optional for backend-focused capabilities."
}
```

---

## 🏆 Best Practices Summary

### ✅ DO

1. **Write API tests first** (before GUI tests)
2. **Default to `ApiTests` evidence level** for most capabilities
3. **Use `SafeOperationsOnly` for runtime validation**
4. **Track evidence source** (`CiCdPipeline` > `LocalTestRunner`)
5. **Accept capabilities without GUI** (`guiRoute: null` is OK!)
6. **Curate registry manually** (don't auto-generate)
7. **Prioritize integration tests** for cross-system flows

### ❌ DON'T

1. **Don't require GUI for all capabilities**
2. **Don't use GUI tests as primary evidence**
3. **Don't run unsafe operations in runtime validation**
4. **Don't confuse evidence types:**
   - `GuiScreenshot` ≠ verification (just documentation)
   - `ManualVerification` ≠ scalable (use sparingly)
5. **Don't treat registry as discovery** (it's declaration)
6. **Don't skip API tests** thinking GUI tests are enough

---

## 📚 Example: Well-designed Capability (API-first)

```json
{
  "id": "course-management",
  "name": "Course Management",
  "description": "Full lifecycle management of courses via API",
  "category": "Courses",
  "status": "ApiVerified",

  // ✅ CORE: API operations
  "apiOperationIds": [
    "GetCourses",
    "CreateCourse",
    "UpdateCourse",
    "ApproveCourse",
    "ArchiveCourse"
  ],

  // ✅ PRIMARY EVIDENCE: API automated tests
  "apiTestIds": [
    "CourseTests.GetAll_ReturnsValidSchema",
    "CourseTests.Create_ValidatesInput",
    "CourseTests.Update_UpdatesSuccessfully",
    "CourseTests.Approve_RequiresManagerRole",
    "CourseTests.Archive_SoftDeletes"
  ],

  // ⚪ OPTIONAL: GUI (happens to exist)
  "guiRoute": "/courses",
  "guiFeature": "Courses Management Page",

  // ⚪ TERTIARY EVIDENCE: GUI tests (optional)
  "guiTestIds": [
    "CourseUiTests.CanCreateCourse",
    "CourseUiTests.CanApproveCourse"
  ],

  // Evidence level: API tests sufficient
  "requiredEvidenceLevel": "ApiTests",

  // Metadata
  "metadata": {
    "owner": "LearningSquad",
    "priority": "High",
    "apiVersion": "v1",
    "apiDocumentation": "https://api.internal/docs/courses"
  }
}
```

**Evidence recorded:**
```
ApiAutomatedTest (5x) ← PRIMARY ✅
IntegrationTest (1x)  ← IMPORTANT ✅
GuiAutomatedTest (2x) ← TERTIARY (nice to have)
ApiExecution (5x)     ← Runtime monitoring

Status: ApiVerified ✅ (production-ready!)
```

---

**Sammanfattning:** API-first betyder att **API operations och API tests** är kärnan. GUI, screenshots, och manuell verifiering är sekundära eller dokumentationssyften. Registry är en **förberedd deklaration**, inte auto-discovery. Runtime validation är **selektiv** för att undvika side effects.

# Search for test files
   Get-ChildItem -Path "..\..\tests" -Recurse -Filter "*Tests.cs" | 
     Select-String -Pattern "\[Fact\]" -Context 0,1
