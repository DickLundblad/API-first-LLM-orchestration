# How to Fill the Capability Registry

This guide shows you how to populate `CapabilityRegistry.json` with your actual system capabilities.

## Quick Start: Auto-Generate from Existing Data

### Step 1: Run the Generator Script

```powershell
cd src\ApiFirst.LlmOrchestration.McpServer
..\..\tools\Generate-CapabilityRegistry.ps1
```

This will:
- ✅ Read your `GuiSupportMappings.json`
- ✅ Group operations by GUI route (identifies logical capabilities)
- ✅ Generate `CapabilityRegistry-generated.json` with skeleton capabilities

### Step 2: Review and Enhance the Generated File

Open `CapabilityRegistry-generated.json` and update:

```json
{
  "id": "team-detail",  // ✓ Good auto-generated
  "name": "Team Member Detail",  // ✓ Good
  "description": "Manage team member detail via API",  // ⚠️ Update to be more specific
  "category": "Team",  // ✓ Good
  "status": "Planned",  // 🔴 UPDATE THIS!
  "apiOperationIds": ["GetTeamMember", "UpdateTeamMember", "DeleteTeamMember"],  // ✓ Good
  "apiTestIds": [],  // 🔴 ADD YOUR TESTS!
  "guiRoute": "/team/{id}",  // ✓ Good
  "guiFeature": "Team Member Detail",  // ✓ Good
  "guiTestIds": [],  // ⚪ Add if you have GUI tests
  "backlogItemIds": [],  // ⚪ Add if you track in issue tracker
  "requiredEvidenceLevel": "ApiTests",  // ✓ Good default
  "metadata": {
    "owner": "TODO",  // 🔴 UPDATE THIS!
    "priority": "Medium",  // ⚪ Update if needed
    "generatedFrom": "GuiSupportMappings"
  }
}
```

### Step 3: Find Your Test IDs

Look in your test project for test methods:

```powershell
# Find all test methods in your test project
Get-ChildItem -Path "tests\ApiFirst.LlmOrchestration.Tests" -Recurse -Filter "*.cs" | 
  Select-String -Pattern "\[Fact\]|\[Theory\]" -Context 0,1
```

**Example test file:**
```csharp
// tests/ApiFirst.LlmOrchestration.Tests/TeamMemberTests.cs

[Fact]
public void GetTeamMembers_ReturnsAllMembers() { ... }

[Fact]
public void UpdateTeamMember_UpdatesSuccessfully() { ... }
```

**Add to capability:**
```json
{
  "apiTestIds": [
    "TeamMemberTests.GetTeamMembers_ReturnsAllMembers",
    "TeamMemberTests.UpdateTeamMember_UpdatesSuccessfully"
  ]
}
```

### Step 4: Set Capability Status

Based on your actual implementation:

| Status | When to Use |
|--------|-------------|
| `Planned` | Capability is designed but not implemented |
| `InProgress` | API code being written |
| `ApiImplemented` | API exists but no automated tests yet |
| `ApiVerified` | ✅ API + automated tests working (MINIMUM for production) |
| `FullyVerified` | API + automated tests + GUI tests (if GUI exists) |
| `Deprecated` | Scheduled for removal |

### Step 5: Rename and Use

```powershell
# After reviewing and updating
Move-Item CapabilityRegistry-generated.json CapabilityRegistry.json -Force
```

---

## Manual Method: Building from Scratch

If you prefer to build capabilities manually or need more control:

### Step 1: Identify Business Use Cases

Think about **what users want to accomplish**, not just API endpoints.

**Examples:**
- ❌ Bad: "GET /api/team-members endpoint"
- ✅ Good: "Team Member Management" (includes GET, POST, PUT, DELETE)

**Questions to ask:**
1. What business goal does this serve?
2. What operations work together to accomplish this goal?
3. Is there a GUI for this? (optional)
4. Do we have tests for this?

### Step 2: Create Capability Structure

```json
{
  "id": "your-capability-id",
  "name": "Human Readable Name",
  "description": "What this capability does for users (be specific!)",
  "category": "Team|Courses|Security|Search|Admin|etc",
  "status": "Planned|InProgress|ApiImplemented|ApiVerified|FullyVerified|Deprecated",

  // REQUIRED - Core API operations
  "apiOperationIds": [
    "OperationId1",
    "OperationId2"
  ],

  // PRIMARY EVIDENCE - Your API tests
  "apiTestIds": [
    "TestClass.TestMethod1",
    "TestClass.TestMethod2"
  ],

  // OPTIONAL - GUI info (if exists)
  "guiRoute": "/route",
  "guiFeature": "Feature Name",

  // TERTIARY EVIDENCE - GUI tests (if exists)
  "guiTestIds": [
    "UiTestClass.TestMethod"
  ],

  // OPTIONAL - Traceability
  "backlogItemIds": [
    "STORY-123",
    "EPIC-456"
  ],

  // Evidence level required
  "requiredEvidenceLevel": "ApiExecution|ApiTests|ApiAndGuiTests|Comprehensive",

  // Metadata
  "metadata": {
    "owner": "TeamName",
    "priority": "High|Medium|Low",
    "apiVersion": "v1",
    "apiDocumentation": "https://api.internal/docs/endpoint"
  }
}
```

### Step 3: Common Capability Patterns

#### Pattern 1: CRUD Capability (Create, Read, Update, Delete)

```json
{
  "id": "team-member-management",
  "name": "Team Member Management",
  "description": "Full lifecycle management of team members via API",
  "category": "Team",
  "status": "ApiVerified",
  "apiOperationIds": [
    "GetTeamMembers",      // List
    "GetTeamMember",       // Read
    "CreateTeamMember",    // Create
    "UpdateTeamMember",    // Update
    "DeleteTeamMember"     // Delete
  ],
  "apiTestIds": [
    "TeamMemberTests.GetAll_ReturnsAllMembers",
    "TeamMemberTests.GetById_ReturnsSpecificMember",
    "TeamMemberTests.Create_CreatesSuccessfully",
    "TeamMemberTests.Update_UpdatesSuccessfully",
    "TeamMemberTests.Delete_DeletesSuccessfully"
  ],
  "guiRoute": "/team",
  "guiTestIds": [
    "TeamUiTests.CanViewList",
    "TeamUiTests.CanEditMember"
  ],
  "requiredEvidenceLevel": "ApiTests"
}
```

#### Pattern 2: Read-Only Capability

```json
{
  "id": "team-member-listing",
  "name": "Team Member Listing",
  "description": "View and search team members (read-only)",
  "category": "Team",
  "status": "ApiVerified",
  "apiOperationIds": [
    "GetTeamMembers",
    "SearchTeamMembers"
  ],
  "apiTestIds": [
    "TeamMemberTests.GetAll_ReturnsAllMembers",
    "TeamMemberTests.Search_FiltersCorrectly"
  ],
  "guiRoute": "/team",
  "requiredEvidenceLevel": "ApiTests"
}
```

#### Pattern 3: Workflow Capability

```json
{
  "id": "course-approval-workflow",
  "name": "Course Approval Workflow",
  "description": "Submit, review, and approve course proposals",
  "category": "Courses",
  "status": "ApiVerified",
  "apiOperationIds": [
    "CreateCourse",        // Submit
    "GetCourse",           // Review
    "ApproveCourse",       // Approve
    "RejectCourse"         // Reject
  ],
  "apiTestIds": [
    "CourseWorkflowTests.SubmitProposal_CreatesInPendingState",
    "CourseWorkflowTests.Approve_RequiresManagerRole",
    "CourseWorkflowTests.Approve_ChangesStatus",
    "CourseWorkflowTests.Reject_RequiresReason"
  ],
  "guiRoute": "/courses/approval",
  "requiredEvidenceLevel": "ApiTests",
  "metadata": {
    "owner": "LearningSquad",
    "priority": "High",
    "requiresRole": "Manager"
  }
}
```

#### Pattern 4: Backend-Only Capability (No GUI)

```json
{
  "id": "email-notification-service",
  "name": "Email Notification Service",
  "description": "Send automated email notifications (backend service)",
  "category": "Notifications",
  "status": "ApiVerified",
  "apiOperationIds": [
    "SendEmailNotification",
    "GetNotificationStatus"
  ],
  "apiTestIds": [
    "NotificationTests.Send_SendsEmailSuccessfully",
    "NotificationTests.Send_HandlesFailuresGracefully",
    "NotificationTests.GetStatus_ReturnsCorrectState"
  ],
  "guiRoute": null,  // No GUI - backend only
  "guiFeature": null,
  "guiTestIds": null,
  "requiredEvidenceLevel": "ApiTests",  // API tests are sufficient
  "metadata": {
    "owner": "PlatformSquad",
    "priority": "Critical",
    "isBackendOnly": true
  }
}
```

### Step 4: Validate Your Capabilities

```powershell
# Test loading the registry
cd src\ApiFirst.LlmOrchestration.McpServer
dotnet run -- --swagger-url http://localhost:5000/api/swagger.json

# In another terminal, test MCP tools
# List capabilities
{"method":"tools/call","params":{"name":"list_capabilities","arguments":{}}}

# Get specific capability
{"method":"tools/call","params":{"name":"get_capability","arguments":{"capabilityId":"team-member-management"}}}
```

---

## Tips and Best Practices

### Naming Conventions

**Capability IDs** (lowercase-with-hyphens):
- ✅ `team-member-management`
- ✅ `course-enrollment`
- ✅ `email-notifications`
- ❌ `TeamMemberManagement` (use lowercase)
- ❌ `get-team-members` (too specific, think use case)

**Capability Names** (Title Case):
- ✅ `Team Member Management`
- ✅ `Course Approval Workflow`
- ❌ `Team member management` (capitalize all major words)

### Grouping Operations

**Good grouping** (related operations that form a complete use case):
```json
{
  "id": "course-management",
  "apiOperationIds": ["GetCourses", "CreateCourse", "UpdateCourse", "ArchiveCourse"]
}
```

**Bad grouping** (unrelated operations):
```json
{
  "id": "mixed-capability",
  "apiOperationIds": ["GetCourses", "GetTeamMembers", "Login"]  // ❌ Too mixed!
}
```

### Setting Evidence Levels

| Evidence Level | When to Use | Example |
|----------------|-------------|---------|
| `ApiExecution` | Just need runtime check | Health checks, monitoring endpoints |
| `ApiTests` | **DEFAULT** - Most capabilities | Standard CRUD operations |
| `ApiAndGuiTests` | Critical user-facing features with GUI | User registration, payment processing |
| `Comprehensive` | High-traffic or security-critical | Authentication, authorization |

### Categories

Organize by domain or functional area:

- `Team` - Team member management
- `Courses` - Course management, catalog
- `Enrollment` - Course enrollments
- `Security` - Authentication, authorization
- `Notifications` - Email, SMS, push notifications
- `Search` - Search and filtering capabilities
- `Admin` - Administrative functions
- `Reporting` - Reports and analytics
- `Integration` - External system integrations

---

## Checklist: Is Your Capability Complete?

Before adding a capability, ensure:

- [ ] **ID** is unique and lowercase-with-hyphens
- [ ] **Name** clearly describes the business use case
- [ ] **Description** explains what users can accomplish
- [ ] **Category** groups it with related capabilities
- [ ] **Status** reflects actual implementation state
- [ ] **apiOperationIds** lists all related API operations
- [ ] **apiTestIds** lists actual test class.method names (if tests exist)
- [ ] **guiRoute** is set only if GUI actually exists
- [ ] **requiredEvidenceLevel** is appropriate for the capability
- [ ] **metadata.owner** identifies the responsible team
- [ ] **metadata.priority** reflects business importance

---

## Example: Complete Process

Let's walk through filling a real capability:

### 1. Identify the Use Case

Looking at your API, you notice:
- `GetCourses` - Lists all courses
- `GetCourse` - Gets a single course
- `CreateCourse` - Creates a new course
- `UpdateCourse` - Updates a course
- `ApproveCourse` - Approves a pending course
- `ArchiveCourse` - Archives an old course

**Use case**: "Course Management" (full lifecycle)

### 2. Check for Tests

```powershell
# Find course-related tests
Get-ChildItem -Path "tests" -Recurse -Filter "*Course*.cs" | Select-String -Pattern "\[Fact\]" -Context 0,1
```

Found:
- `CourseTests.GetAll_ReturnsAllCourses`
- `CourseTests.Create_ValidatesInput`
- `CourseTests.Update_UpdatesSuccessfully`
- `CourseTests.Approve_RequiresManagerRole`

### 3. Check for GUI

Looking at `GuiSupportMappings.json`, found:
- `guiRoute: "/courses"`
- Multiple operations map to this route

### 4. Check Backlog

Looking at your issue tracker:
- Story #456: "Implement course catalog"
- Story #457: "Add course approval workflow"

### 5. Determine Status

- ✅ API exists
- ✅ Tests exist and pass
- ✅ GUI exists
- Decision: Status = `ApiVerified` (or `FullyVerified` if you have GUI tests)

### 6. Create Capability

```json
{
  "id": "course-management",
  "name": "Course Management",
  "description": "Full lifecycle management of courses including creation, editing, approval, and archival",
  "category": "Courses",
  "status": "ApiVerified",
  "apiOperationIds": [
    "GetCourses",
    "GetCourse",
    "CreateCourse",
    "UpdateCourse",
    "ApproveCourse",
    "ArchiveCourse"
  ],
  "apiTestIds": [
    "CourseTests.GetAll_ReturnsAllCourses",
    "CourseTests.GetById_ReturnsSpecificCourse",
    "CourseTests.Create_ValidatesInput",
    "CourseTests.Create_CreatesSuccessfully",
    "CourseTests.Update_UpdatesSuccessfully",
    "CourseTests.Approve_RequiresManagerRole",
    "CourseTests.Archive_SoftDeletes"
  ],
  "guiRoute": "/courses",
  "guiFeature": "Course Management Page",
  "guiTestIds": [
    "CourseUiTests.CanCreateCourse",
    "CourseUiTests.CanEditCourse"
  ],
  "backlogItemIds": [
    "STORY-456",
    "STORY-457"
  ],
  "requiredEvidenceLevel": "ApiTests",
  "metadata": {
    "owner": "LearningSquad",
    "priority": "High",
    "apiVersion": "v1",
    "requiresRole": "Manager (for approval)"
  }
}
```

### 7. Test It

```powershell
cd src\ApiFirst.LlmOrchestration.McpServer
dotnet run

# Test the capability
{"method":"tools/call","params":{"name":"get_capability","arguments":{"capabilityId":"course-management"}}}
```

---

## Need Help?

- See [API_FIRST_PRINCIPLES.md](API_FIRST_PRINCIPLES.md) for design principles
- See [QUICKSTART.md](QUICKSTART.md) for getting started
- See example capabilities in `CapabilityRegistry.json`
