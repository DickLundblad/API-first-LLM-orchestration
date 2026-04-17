# Quickstart: Capability Registry

## What Was Implemented?

✅ **Capability Registry** - Central registry of system capabilities  
✅ **Runtime Validator** - Verifies APIs actually work  
✅ **MCP Server Integration** - New tools for LLM interaction  
✅ **Evidence Tracking** - Tracks proof for each capability  

## New MCP Tools

### 1. list_capabilities
List all capabilities, filter by category or status.

**Example:**
```json
{
  "method": "tools/call",
  "params": {
    "name": "list_capabilities",
    "arguments": {
      "category": "Team",
      "status": "ApiVerified"
    }
  }
}
```

### 2. get_capability
Get detailed info about a capability including evidence.

**Example:**
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

### 3. alidate_capability
Run runtime validation of a capability.

**Example:**
```json
{
  "method": "tools/call",
  "params": {
    "name": "validate_capability",
    "arguments": {
      "capabilityId": "team-member-management",
      "swaggerUrl": "http://localhost:5000/api/swagger.json",
      "apiBaseUrl": "http://localhost:5000",
      "scope": "SafeOperationsOnly"
    }
  }
}
```

### 4. capability_health
Health report of all capabilities.

**Example:**
```json
{
  "method": "tools/call",
  "params": {
    "name": "capability_health",
    "arguments": {}
  }
}
```

## File Structure

```
src/
├── ApiFirst.LlmOrchestration/
│   └── Registry/
│       ├── Capability.cs                    # ✨ NEW
│       ├── CapabilityRegistry.cs            # ✨ NEW
│       └── RuntimeCapabilityValidator.cs    # ✨ NEW
│
└── ApiFirst.LlmOrchestration.McpServer/
    ├── McpServer.cs                         # 🔧 UPDATED
    ├── CapabilityRegistry.json              # ✨ NEW
    ├── GuiSupportMappings.json              # ✓ Existing
    └── appsettings.json                     # ✓ Existing
```

## Next Steps

### 1. Update CapabilityRegistry.json
Add all your use cases to `src/ApiFirst.LlmOrchestration.McpServer/CapabilityRegistry.json`.

**Template:**
```json
{
  "id": "your-capability-id",
  "name": "Your Capability Name",
  "description": "What this capability does",
  "category": "CategoryName",
  "status": "ApiVerified",
  "apiOperationIds": ["Operation1", "Operation2"],
  "apiTestIds": ["TestClass.TestMethod"],
  "guiRoute": "/your-route",
  "guiTestIds": ["UiTestClass.TestMethod"],
  "backlogItemIds": ["STORY-123"],
  "requiredEvidenceLevel": "ApiTests"
}
```

### 2. Test MCP Server
```powershell
cd src\ApiFirst.LlmOrchestration.McpServer
dotnet run -- --swagger-url http://localhost:5000/api/swagger.json
```

### 3. Connect to LLM
Use MCP tools from your LLM client to:
- List capabilities
- Validate runtime status
- Get health reports

### 4. Integrate with Tests
Report test results as evidence:

```csharp
_capabilityRegistry.RecordEvidence(new CapabilityEvidence(
    "team-member-management",
    EvidenceType.ApiAutomatedTest,  // PRIMARY
    EvidenceStatus.Success,
    EvidenceSource.CiCdPipeline,
    DateTime.UtcNow,
    "API: All tests passed"));
```

### 5. Screenshot Integration
You already have the `ScreenshotCapture` project - connect it for GUI evidence:

```csharp
_capabilityRegistry.RecordEvidence(new CapabilityEvidence(
    "team-member-management",
    EvidenceType.GuiScreenshot,  // DOCUMENTATION
    EvidenceStatus.Success,
    EvidenceSource.LocalTestRunner,
    DateTime.UtcNow,
    "Screenshot captured",
    new Dictionary<string, object> { ["url"] = screenshotUrl }));
```

## Example: Complete Flow

1. **LLM asks**: "What team management features exist?"
2. **MCP**: `list_capabilities --category Team`
3. **Registry**: Returns `team-member-management` capability
4. **LLM asks**: "Does it work right now?"
5. **MCP**: `validate_capability --capabilityId team-member-management`
6. **Validator**: Calls GetTeamMembers, UpdateTeamMember, etc.
7. **Registry**: Saves evidence
8. **LLM gets**: "Verified - all 4 operations working"

## Architecture

```
┌─────────────────┐
│   LLM Client    │
└────────┬────────┘
         │ MCP Protocol
         ↓
┌─────────────────────────────────────┐
│        MCP Server                   │
│  ┌──────────────────────────────┐  │
│  │  Capability Tools            │  │
│  │  - list_capabilities         │  │
│  │  - get_capability            │  │
│  │  - validate_capability       │  │
│  │  - capability_health         │  │
│  └──────────────┬───────────────┘  │
└─────────────────┼───────────────────┘
                  │
         ┌────────┴────────┐
         ↓                 ↓
┌──────────────────┐  ┌──────────────────┐
│ Capability       │  │ Runtime          │
│ Registry         │←─│ Validator        │
│                  │  │                  │
│ - Capabilities   │  │ - API Calls      │
│ - Evidence       │  │ - Verification   │
│ - Mappings       │  │ - Evidence Log   │
└──────────────────┘  └─────────┬────────┘
                                ↓
                        ┌───────────────┐
                        │  Target API   │
                        └───────────────┘
```

## Benefits

✅ **Single Source of Truth**: Registry knows everything system can do  
✅ **Evidence-Based**: All claims backed by proof  
✅ **LLM-Friendly**: MCP tools for direct LLM interaction  
✅ **Traceability**: API → GUI → Tests → Backlog  
✅ **Runtime Verification**: Know if things actually work  
✅ **Self-Documenting**: Living documentation  
✅ **API-First**: API operations and tests are core; GUI is optional

## Documentation

- Full documentation: [CAPABILITY_REGISTRY_ARCHITECTURE.md](CAPABILITY_REGISTRY_ARCHITECTURE.md)
- API-first principles: [API_FIRST_PRINCIPLES.md](API_FIRST_PRINCIPLES.md)
- Complete summary: [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md)
- Visual diagrams: [ARCHITECTURE_DIAGRAMS.md](ARCHITECTURE_DIAGRAMS.md)
- Implementation guide: [IMPLEMENTATION_CHECKLIST.md](IMPLEMENTATION_CHECKLIST.md)
