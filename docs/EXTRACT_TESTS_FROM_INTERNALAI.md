# Extracting Test IDs from InternalAI Repository

This guide shows you how to automatically extract test IDs from your **InternalAI** repository (the system under test) and populate the Capability Registry.

## Quick Start

### Step 1: Extract Test IDs

```powershell
# From the API-first-LLM-orchestration repo root
.\tools\Extract-TestIds-FromInternalAI.ps1
```

This will:
- ✅ Scan `C:\git\InternalAI` for test projects
- ✅ Extract all test methods
- ✅ Infer which capability each test belongs to
- ✅ Save mapping to `test-mapping.json`

**Output example:**
```
🔍 Scanning InternalAI repository at: C:\git\InternalAI

Found 3 test project(s):
  - C:\git\InternalAI\tests\InternalAI.Api.Tests
  - C:\git\InternalAI\tests\InternalAI.Core.Tests
  - C:\git\InternalAI\tests\InternalAI.Integration.Tests

📂 Processing: InternalAI.Api.Tests
  ✓ TeamMemberControllerTests.GetAllMembers_ReturnsOk → Team Management
  ✓ TeamMemberControllerTests.GetMemberById_ReturnsOk → Team Management
  ✓ TeamMemberControllerTests.UpdateMember_UpdatesSuccessfully → Team Management
  ✓ CourseControllerTests.GetAllCourses_ReturnsOk → Course Management
  ✓ CourseControllerTests.CreateCourse_CreatesSuccessfully → Course Management
  ...

📊 SUMMARY: Found 47 tests

  Team Management: 12 tests (25.5%)
  Course Management: 18 tests (38.3%)
  Course Enrollment: 8 tests (17.0%)
  Authentication: 9 tests (19.1%)
```

### Step 2: View Suggested Updates

```powershell
.\tools\Extract-TestIds-FromInternalAI.ps1 -ShowSuggestions
```

This will show you exactly what to add to `CapabilityRegistry.json`:

```
📋 SUGGESTED UPDATES FOR CapabilityRegistry.json

📦 Capability ID: "team-member-management"
   Found 12 tests:

   "apiTestIds": [
     "TeamMemberControllerTests.GetAllMembers_ReturnsOk",
     "TeamMemberControllerTests.GetMemberById_ReturnsOk",
     "TeamMemberControllerTests.UpdateMember_UpdatesSuccessfully",
     "TeamMemberControllerTests.DeleteMember_DeletesSuccessfully",
     "TeamMemberServiceTests.GetMembers_ReturnsAllMembers",
     "TeamMemberServiceTests.UpdateMember_ValidatesInput",
     ...
   ]
```

### Step 3: Auto-Update Capability Registry

```powershell
.\tools\Extract-TestIds-FromInternalAI.ps1 -UpdateCapabilityRegistry
```

This will:
- ✅ Read your current `CapabilityRegistry.json`
- ✅ Match test IDs to capabilities automatically
- ✅ Add `apiTestIds` arrays
- ✅ Update status from `Planned` → `ApiVerified` if tests exist
- ✅ Save the updated file

**Output:**
```
🔄 UPDATING CapabilityRegistry.json

✅ Updated 'team-member-management': Added 12 tests, status → ApiVerified
✅ Updated 'course-management': Added 18 tests, status → ApiVerified
✅ Updated 'course-enrollment': Added 8 tests
⚪ No tests found for 'advanced-search'

💾 Updated 3 capabilities in CapabilityRegistry.json
```

## Advanced Usage

### Different InternalAI Path

```powershell
.\tools\Extract-TestIds-FromInternalAI.ps1 -InternalAIPath "D:\Projects\InternalAI"
```

### Custom Test Project Pattern

```powershell
# Only scan projects with "Api.Tests" in the name
.\tools\Extract-TestIds-FromInternalAI.ps1 -TestProjectPattern "*Api.Tests*"
```

### Save to Different Location

```powershell
.\tools\Extract-TestIds-FromInternalAI.ps1 -OutputPath ".\my-test-mapping.json"
```

### Full Workflow

```powershell
# 1. Extract and show suggestions
.\tools\Extract-TestIds-FromInternalAI.ps1 -ShowSuggestions

# 2. Review the suggestions, then auto-update
.\tools\Extract-TestIds-FromInternalAI.ps1 -UpdateCapabilityRegistry

# 3. Test the updated registry
cd src\ApiFirst.LlmOrchestration.McpServer
dotnet run -- --swagger-url http://localhost:5000/api/swagger.json

# 4. Check capability health
# In another terminal:
{"method":"tools/call","params":{"name":"capability_health","arguments":{}}}
```

## How Test Inference Works

The script automatically maps tests to capabilities based on file names:

| Test File Pattern | Inferred Capability |
|------------------|---------------------|
| `*Team*.cs`, `*Member*.cs` | `team-member-management` |
| `*Course*.cs` (not enrollment) | `course-management` |
| `*Enroll*.cs`, `*Registration*.cs` | `course-enrollment` |
| `*Auth*.cs`, `*Login*.cs`, `*Password*.cs` | `authentication` |
| `*Admin*.cs` | `administration` |

### Manual Mapping

If the automatic inference doesn't work for your tests, you can:

1. Run with `-ShowSuggestions` to see what was found
2. Manually copy test IDs to the correct capability in `CapabilityRegistry.json`
3. Or modify the script's inference logic (search for `# Infer feature/capability`)

## Test Mapping File Format

The generated `test-mapping.json` looks like this:

```json
{
  "generatedDate": "2025-06-18 14:30:00",
  "internalAIPath": "C:\\git\\InternalAI",
  "totalTests": 47,
  "testsByCapability": {
    "team-member-management": [
      "TeamMemberControllerTests.GetAllMembers_ReturnsOk",
      "TeamMemberControllerTests.UpdateMember_UpdatesSuccessfully"
    ],
    "course-management": [
      "CourseControllerTests.GetAllCourses_ReturnsOk",
      "CourseControllerTests.CreateCourse_CreatesSuccessfully"
    ]
  }
}
```

You can use this for:
- Reference when manually updating capabilities
- Auditing which tests belong to which capability
- Tracking test coverage over time

## Troubleshooting

### "InternalAI repository not found"

Make sure InternalAI is cloned at `C:\git\InternalAI`, or specify the correct path:

```powershell
.\tools\Extract-TestIds-FromInternalAI.ps1 -InternalAIPath "C:\your\path\to\InternalAI"
```

### "No test projects found"

The script looks for directories with names like `*Tests*` or containing `.csproj` files with "Test" in the directory name. Check that your test projects follow this pattern.

### "Tests not being detected"

The script supports:
- **NUnit**: `[Test]`, `[TestCase]`
- **xUnit**: `[Fact]`, `[Theory]`
- **MSTest**: `[TestMethod]`

If your tests use different attributes, you may need to modify the regex pattern in the script.

### Wrong capability inference

Edit the script at the `# Infer feature/capability` section to add your own mapping logic:

```powershell
# Add custom mapping
elseif ($file.Name -match 'Notification|Email') {
    $feature = "Notifications"
    $capabilityId = "email-notifications"
}
```

## Integration with CI/CD

You can run this script in your CI/CD pipeline to keep test IDs up-to-date:

```yaml
# Example GitHub Actions / Azure DevOps step
- name: Update Capability Registry with Latest Tests
  run: |
    cd API-first-LLM-orchestration
    .\tools\Extract-TestIds-FromInternalAI.ps1 -UpdateCapabilityRegistry
    git diff src/ApiFirst.LlmOrchestration.McpServer/CapabilityRegistry.json
```

## Next Steps

After extracting test IDs:

1. ✅ Review `CapabilityRegistry.json` - ensure test IDs look correct
2. ✅ Update capability **status**:
   - If tests exist and pass → `ApiVerified`
   - If tests exist but failing → `ApiImplemented`
3. ✅ Add **GUI test IDs** separately if you have UI tests
4. ✅ Test the MCP server to verify everything works

See [HOW_TO_FILL_CAPABILITY_REGISTRY.md](HOW_TO_FILL_CAPABILITY_REGISTRY.md) for complete guidance.
