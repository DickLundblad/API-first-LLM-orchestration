# 🔧 DEBUG: Evidence-problemet

## Problem

MCP-servern rapporterade:
```
[CapabilityRegistry] Generated 51 capabilities from Swagger
[TestMapping] Linked 201 tests to 36 capabilities
[TestMapping] 7 capabilities now have evidence  ← FÖR LÅGT!
```

## Möjliga orsaker

### 1. Capability ID mismatch
TestMappings.json använder lowercase (rätt):
```json
{"capabilities": ["login", "getteammembers"]}
```

Men capabilities kanske genereras annorlunda från Swagger?

### 2. Operation ID case sensitivity
CapabilityGenerator skapar:
```csharp
Id = operation.OperationId.ToLowerInvariant()  // "login"
```

Men `GetCapabilitiesByOperation` kanske inte hittar med case-insensitivity?

### 3. Evidence skapas men meetsEvidenceRequirement kollar fel
Evidence skapas men `MeetsEvidenceRequirement()` kanske har bugg?

## Debug-logging tillagt

Uppdaterade `ExternalTestMapper.cs` med mer logging:

```csharp
Console.Error.WriteLine($"[TestMapping] Debug - Direct capability matches: found=X, notfound=Y");
Console.Error.WriteLine($"[TestMapping] Debug - Via operation matches: Z");
Console.Error.WriteLine($"[TestMapping] Debug - Unique capabilities to update: W");
Console.Error.WriteLine($"[TestMapping] Debug - Created X evidence records");
Console.Error.WriteLine($"[TestMapping] Warning: Capability 'X' not found in registry");
```

## Starta om MCP-servern med debug

```powershell
.\Start-McpServer.ps1
```

**Leta efter dessa rader:**
```
[TestMapping] Debug - Direct capability matches: found=XX, notfound=YY
[TestMapping] Debug - Via operation matches: ZZ
[TestMapping] Debug - Created XX evidence records
[TestMapping] Warning: Capability 'xxx' not found in registry
```

## Förväntade värden

Med 70 tests och 43 operations:

**Optimalt:**
- Direct found: ~140 (70 tests × 2 capabilities average)
- Via operation: ~130 (70 tests × 43 operations matched)
- Evidence created: ~200+
- Capabilities with evidence: ~43 (alla)

**Om du ser:**
- "notfound" > 50 → Capability IDs matchar inte
- Evidence created < 100 → Få capabilities hittas
- Warnings om "not found" → Visa vilka IDs som inte finns

## Nästa steg

1. Kör servern och kopiera ALL debug-output här
2. Jag analyserar exakt varför bara 7 får evidence
3. Fixar den exakta orsaken

## Quick check nu

Kör detta för att se capability IDs som genereras:

```powershell
# Start server, then from LLM:
"Lista alla capabilities" 
→ Kopiera de första 10 capability IDs här
```

Jämför med TestMappings.json capabilities för att se mismatch.
