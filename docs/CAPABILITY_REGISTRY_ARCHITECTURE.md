# API-first LLM Orchestration - Capability Registry Architecture

## Overview

This system implements a three-layer architecture for MCP (Model Context Protocol) with focus on capability management and runtime validation.

See [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md) for a complete overview of what was implemented.
See [API_FIRST_PRINCIPLES.md](API_FIRST_PRINCIPLES.md) for detailed API-first design principles.

## Quick Architecture Reference

### Three Layers

1. **MCP Server** → Exposes capabilities to LLM via protocol
2. **Capability Registry** → Declares what system claims to support (prepared index)
3. **Runtime Validator** → Verifies what actually works (selective validation)

### Key MCP Tools

- list_capabilities - List all use case capabilities with filters
- get_capability - Get detailed capability info + evidence + test coverage
- alidate_capability - Selectively validate at runtime (default: safe operations only)
- capability_health - System health report (API-first metrics)

### Evidence Hierarchy (API-first)

1. **ApiAutomatedTest** (PRIMARY) ← Most important
2. **IntegrationTest** (IMPORTANT)
3. **ApiExecution** (SECONDARY)
4. **GuiAutomatedTest** (TERTIARY) ← Only if GUI exists
5. **PerformanceBenchmark** (OPTIONAL)
6. **GuiScreenshot** (DOCUMENTATION) ← Not verification!
7. **ManualVerification** (FALLBACK)

### Capability Status Flow

`
Planned → InProgress → ApiImplemented → ApiVerified ✅ → FullyVerified
                                        (production min)
`

### Validation Scopes

- SafeOperationsOnly (default) ✅ - Only GET/HEAD
- AllOperations ⚠️ - Everything (side effects!)
- None - Don't validate

For complete documentation, see:
- [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md)
- [API_FIRST_PRINCIPLES.md](API_FIRST_PRINCIPLES.md)
- [QUICKSTART.md](QUICKSTART.md)
- [ARCHITECTURE_DIAGRAMS.md](ARCHITECTURE_DIAGRAMS.md)
- [IMPLEMENTATION_CHECKLIST.md](IMPLEMENTATION_CHECKLIST.md)
