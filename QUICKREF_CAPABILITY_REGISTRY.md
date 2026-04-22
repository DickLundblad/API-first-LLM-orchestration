# Quick Reference: Filling the Capability Registry

## ✅ DONE! Your Registry is Already Populated

You've successfully extracted **3,252 tests** from InternalAI and populated your `CapabilityRegistry.json`!

- **893 API tests** (Python backend)
- **2,359 GUI tests** (Frontend)
- **3 capabilities updated** to `ApiVerified` status

## 🔄 To Update Again (After Adding New Tests)

```powershell
cd C:\git\API-first-LLM-orchestration
.\tools\ExtractTests.ps1 -Update
```

## 📊 View Current Status

```powershell
# See all tests extracted
.\tools\ExtractTests.ps1

# See tests organized by capability
.\tools\ExtractTests.ps1 -Show
```

## 🧪 Test the MCP Server

```powershell
cd src\ApiFirst.LlmOrchestration.McpServer
dotnet run -- --swagger-url http://localhost:5000/api/swagger.json
```

Then query capabilities:

```json
// List all capabilities
{"method":"tools/call","params":{"name":"list_capabilities","arguments":{}}}

// Get specific capability with test IDs
{"method":"tools/call","params":{"name":"get_capability","arguments":{"capabilityId":"team-member-management"}}}

// Health report
{"method":"tools/call","params":{"name":"capability_health","arguments":{}}}
```

## 📁 Key Files

| File | Description |
|------|-------------|
| `src/ApiFirst.LlmOrchestration.McpServer/CapabilityRegistry.json` | **Main registry** - populated with test IDs |
| `tools/ExtractTests.ps1` | **Extraction script** - re-run to update |
| `tools/Generate-CapabilityRegistry.ps1` | **Initial generator** - creates skeleton from GUI mappings |

## 📚 Documentation

- [TEST_EXTRACTION_SUCCESS.md](TEST_EXTRACTION_SUCCESS.md) - **This extraction summary**
- [HOW_TO_FILL_CAPABILITY_REGISTRY.md](HOW_TO_FILL_CAPABILITY_REGISTRY.md) - Complete guide
- [API_FIRST_PRINCIPLES.md](API_FIRST_PRINCIPLES.md) - Design principles
- [QUICKSTART.md](QUICKSTART.md) - Getting started
- [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md) - Full implementation details

## 🎯 What You Have Now

### Example: team-member-management

```json
{
  "id": "team-member-management",
  "name": "Team Member Management",
  "status": "ApiVerified",  // ✅ Updated!
  "apiOperationIds": ["GetTeamMembers", "UpdateTeamMember", ...],

  // ✅ 67 real API tests from InternalAI backend
  "apiTestIds": [
    "test_app.py::test_team_endpoint_success",
    "test_permissions.py::test_admin_can_view_all_members",
    // ... 65 more
  ],

  // ✅ 144 real GUI tests from InternalAI frontend
  "guiTestIds": [
    "should display team members",
    "filters by name",
    // ... 142 more
  ],

  "apiTestCoverage": 100%,  // All operations have tests!
  "meetsEvidenceRequirement": true  // ✅
}
```

## 🚀 You're Ready!

Your capability registry is now:
- ✅ Populated with real test IDs
- ✅ Linked to actual backend (Python) tests
- ✅ Linked to actual frontend (React) tests  
- ✅ Ready for MCP server queries
- ✅ Tracking API test coverage
- ✅ Evidence-based and verifiable

**Next:** Start using the MCP server to query capabilities! 🎉
