# Architecture Diagrams

## Overview: Three-Layer Architecture

```
┌───────────────────────────────────────────────────────────────────┐
│                    LLM Client (GPT-4, Claude, etc.)                │
└───────────────────────────────┬───────────────────────────────────┘
                                │
                                │ MCP Protocol (JSON-RPC)
                                │
┌───────────────────────────────▼───────────────────────────────────┐
│                      MCP Server Layer                              │
│  ╔══════════════════════════════════════════════════════════════╗ │
│  ║  MCP Tools (exposed to LLM)                                  ║ │
│  ║  • list_capabilities    → Expose use cases                   ║ │
│  ║  • get_capability       → Query evidence                     ║ │
│  ║  • validate_capability  → Runtime validation                 ║ │
│  ║  • capability_health    → Consolidated response              ║ │
│  ╚══════════════════════════════════════════════════════════════╝ │
│                                                                    │
│  Existing tools:                                                   │
│  • search_operations, list_operations, preview_gui, login, etc.    │
└───────────────────────────────┬───────────────────────────────────┘
                                │
                ┌───────────────┴───────────────┐
                │                               │
                ▼                               ▼
┌───────────────────────────────┐  ┌──────────────────────────────┐
│  Capability Registry Layer    │  │  Runtime Validator Layer     │
│  ═══════════════════════════  │  │  ══════════════════════════  │
│                               │  │                              │
│  What system CLAIMS to        │  │  What system ACTUALLY can    │
│  support                      │  │  do right now                │
│                               │  │                              │
│  ┌─────────────────────────┐ │  │  ┌────────────────────────┐ │
│  │ CapabilityRegistry      │ │  │  │ RuntimeValidator       │ │
│  │ • Capabilities          │ │  │  │ • ValidateCapability   │ │
│  │ • Evidence Store        │ │  │  │ • Selective API Calls  │ │
│  │ • Mappings              │ │←─┼──│ • Evidence Recording   │ │
│  └─────────────────────────┘ │  │  └──────────┬─────────────┘ │
│                               │  │             │               │
│  Data Sources:                │  │             │               │
│  • CapabilityRegistry.json    │  │             ▼               │
│  • GuiSupportMappings.json    │  │   ┌──────────────────────┐ │
│  • Test Results               │  │   │ HttpApiExecutor      │ │
│  • Backlog Items              │  │   │ • Execute Operations │ │
└───────────────────────────────┘  │   └──────────┬───────────┘ │
                                   │              │             │
                                   └──────────────┼─────────────┘
                                                  │
                                                  ▼
                                   ┌─────────────────────────────┐
                                   │   Target API                │
                                   │   • GET /api/team-members   │
                                   │   • POST /api/courses       │
                                   │   • etc.                    │
                                   └─────────────────────────────┘
```

## Capability Data Model

```
┌──────────────────────────────────────────────────────────────┐
│                     UseCaseCapability                        │
├──────────────────────────────────────────────────────────────┤
│  id: "team-member-management"                                │
│  name: "Team Member Management"                              │
│  description: "View, edit, and manage team members via API"  │
│  category: "Team"                                            │
│  status: ApiVerified                                         │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ API Operations (REQUIRED - CORE)                       │  │
│  │ • GetTeamMembers                                       │  │
│  │ • GetTeamMember                                        │  │
│  │ • UpdateTeamMember                                     │  │
│  │ • DeleteTeamMember                                     │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ API Tests (PRIMARY EVIDENCE)                           │  │
│  │ • TeamMemberTests.GetAll_ReturnsValid                  │  │
│  │ • TeamMemberTests.Update_Works                         │  │
│  │ API Test Coverage: 100%                                │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ GUI Mapping (OPTIONAL)                                 │  │
│  │ route: "/team"                                         │  │
│  │ feature: "Team Member List & Detail"                   │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ GUI Tests (TERTIARY EVIDENCE - only if GUI exists)     │  │
│  │ • TeamUiTests.CanViewList                              │  │
│  │ • TeamUiTests.CanEditMember                            │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ Backlog Items                                          │  │
│  │ • STORY-123                                            │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ Evidence (with source and type)                        │  │
│  │ • 2025-06-18 12:34 - ApiAutomatedTest - CiCdPipeline  │  │
│  │ • 2025-06-17 09:15 - IntegrationTest - CiCdPipeline   │  │
│  │ • 2025-06-16 14:22 - GuiAutomatedTest - LocalRunner   │  │
│  └────────────────────────────────────────────────────────┘  │
│  requiredEvidenceLevel: ApiTests                             │
│  lastVerified: 2025-06-18T12:34:56Z                          │
└──────────────────────────────────────────────────────────────┘
```

## Data Flow: Validate Capability

```
┌──────────┐
│   LLM    │  "Can I manage team members right now?"
└─────┬────┘
      │
      │ validate_capability(id: "team-member-management", scope: "SafeOperationsOnly")
      ▼
┌─────────────────┐
│   MCP Server    │
└────┬────────────┘
     │
     │ 1. Lookup capability in registry
     ▼
┌──────────────────┐         ┌─────────────────────┐
│ Capability       │◄────────│  "team-member-      │
│ Registry         │         │   management"       │
└────┬─────────────┘         │  status: ApiVerified│
     │                       │  operations: [...]   │
     │ 2. Return capability  └─────────────────────┘
     ▼
┌──────────────────┐
│ Runtime          │
│ Validator        │
└────┬─────────────┘
     │
     │ 3. For each operation (selective - safe only by default):
     ├──► GetTeamMembers (GET) ──┐  EXECUTE ✅
     ├──► GetTeamMember (GET)  ──┤  EXECUTE ✅
     ├──► UpdateTeamMember (PUT) ─┤  SKIP (unsafe) ⚠️
     └──► DeleteTeamMember (DEL) ─┤  SKIP (unsafe) ⚠️
                                  │
                                  ▼
                    ┌──────────────────┐
                    │  HttpApiExecutor │
                    └────┬─────────────┘
                         │
                         │ 4. Call safe API operations
                         ▼
                    ┌──────────────────┐
                    │   Target API     │
                    │   Returns: 200   │
                    └────┬─────────────┘
                         │
                         │ 5. Result
                         ▼
┌──────────────────┐                  ┌──────────────────────┐
│ Runtime          │                  │  Evidence:           │
│ Validator        │────────────────► │  • GetTeamMembers OK │
└────┬─────────────┘  6. Record       │    (executed)        │
     │                   evidence      │  • GetTeamMember OK  │
     │                                 │    (executed)        │
     │                                 │  • UpdateTeamMem OK  │
     │                                 │    (verified exists) │
     │                                 │  • DeleteTeamMem OK  │
     │                                 │    (verified exists) │
     ▼                                 └──────────────────────┘
┌──────────────────┐
│ Capability       │  Update lastVerified
│ Registry         │
└────┬─────────────┘
     │
     │ 7. Return validation result
     ▼
┌─────────────────┐
│   MCP Server    │  { success: true, operations: [...], scope: "SafeOperationsOnly" }
└────┬────────────┘
     │
     ▼
┌──────────┐
│   LLM    │  "Yes! 2 operations executed, 2 verified to exist (unsafe not executed)"
└──────────┘
```

## Integration Points

```
┌──────────────────────────────────────────────────────────────┐
│                  Capability Registry                         │
└───┬──────────────┬──────────────┬─────────────┬─────────────┘
    │              │              │             │
    │ Reads        │ Reads        │ Writes      │ Writes
    │              │              │             │
    ▼              ▼              ▼             ▼
┌─────────┐  ┌──────────┐  ┌──────────┐  ┌──────────────┐
│  GUI    │  │  Swagger │  │  API     │  │  Runtime     │
│ Support │  │  Catalog │  │  Tests   │  │  Validator   │
│ Provider│  │          │  │  Runner  │  │              │
└─────────┘  └──────────┘  └──────────┘  └──────────────┘
    │              │              │             │
    │ GUI routes   │ API ops      │ Evidence    │ Evidence
    │              │              │ (PRIMARY)   │ (SECONDARY)
    ▼              ▼              ▼             ▼
┌────────────────────────────────────────────────────────┐
│         MCP Server (unified orchestration)             │
└────────────────────────────────────────────────────────┘
```

## Evidence Types (Hierarchy)

```
                    ┌──────────────────┐
                    │    Evidence      │
                    │     Types        │
                    └────────┬─────────┘
                             │
        ┌────────────────────┼───────────────────┐
        │                    │                   │
        ▼                    ▼                   ▼
┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│ API          │    │ Integration  │    │ GUI          │
│ Automated    │    │ Test         │    │ Automated    │
│ Test         │    │              │    │ Test         │
│              │    │ From:Cross-  │    │              │
│ From: Test   │    │ system tests │    │ From:UI Test │
│ Runner       │    │              │    │ Runner       │
│              │    │              │    │              │
│ PRIMARY ✅   │    │ IMPORTANT ✅ │    │ TERTIARY ⚪  │
└──────────────┘    └──────────────┘    └──────────────┘
        │                    │                   │
        └────────────────────┼───────────────────┘
                             │
                             ▼
                    ┌──────────────────┐
                    │  Capability      │
                    │  lastVerified    │
                    │  timestamp       │
                    └──────────────────┘
```

## Validation Scopes

```
┌─────────────────────────────────────────┐
│     ValidationScope Options             │
├─────────────────────────────────────────┤
│                                         │
│  SafeOperationsOnly (DEFAULT) ✅        │
│  • Only GET and HEAD                    │
│  • No side effects                      │
│  • Safe for production validation       │
│                                         │
│  AllOperations ⚠️                        │
│  • GET, POST, PUT, DELETE, etc.         │
│  • Can have side effects!               │
│  • Use with extreme caution             │
│  • Only in test environments            │
│                                         │
│  None                                   │
│  • Don't validate at runtime            │
│  • Just check registry                  │
│                                         │
└─────────────────────────────────────────┘
```
