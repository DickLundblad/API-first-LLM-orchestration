# ✅ KLART: Swagger operationIds + Debug logging

## Vad som gjordes

### 1️⃣ Backend: Lagt till 42 operationIds
**Fil:** `C:\git\InternalAI\backend\swagger.json`

**Resultat:**
- ✅ 1 → 43 operations med operationId
- ✅ Backup: swagger.json.backup
- ✅ 100% coverage av alla endpoints

### 2️⃣ MCP Server: Förbättrad logging
**Filer:** 
- `ExternalTestMapper.cs` - Debug logging
- `McpServer.cs` - Visa sample capability IDs

**Ny output:**
```
[CapabilityRegistry] Sample IDs: login, getteammembers, ...
[TestMapping] Debug - Direct capability matches: found=XX, notfound=YY
[TestMapping] Debug - Via operation matches: ZZ
[TestMapping] Debug - Created XX evidence records
[TestMapping] Warning: Capability 'xxx' not found
```

---

## 🚀 TESTA NU

### Steg 1: Starta om backend
```bash
cd C:\git\InternalAI\backend
python app.py
```

Verifiera Swagger har alla operations:
```bash
curl http://localhost:5000/api/swagger.json | jq '.paths | length'
# Ska visa 37 paths

curl http://localhost:5000/api/swagger.json | jq '[.paths[][] | select(.operationId)] | length'
# Ska visa 43 operations
```

### Steg 2: Starta om MCP-server
```powershell
cd C:\git\API-first-LLM-orchestration
.\Start-McpServer.ps1
```

**Kopiera HELA output här**, speciellt:
```
[CapabilityRegistry] Generated X capabilities from Swagger
[CapabilityRegistry] Sample IDs: ...
[TestMapping] Debug - Direct capability matches: ...
[TestMapping] Debug - Via operation matches: ...
[TestMapping] Debug - Created X evidence records
[TestMapping] X capabilities now have evidence
```

### Steg 3: Från LLM-klient
```
"Lista alla capabilities"
```

Kopiera de första 10 capability IDs.

---

## 📊 Förväntat resultat

Med 43 operations i Swagger och 70 tests:

### Optimalt scenario:
```
[CapabilityRegistry] Generated 86 capabilities from Swagger
  (43 individual + 43 grouped by resource)
[CapabilityRegistry] Sample IDs: health, login, register, logout, getauthstatus, ...
[TestMapping] Debug - Direct found: 140, notfound: 0
[TestMapping] Debug - Via operation: 130
[TestMapping] Debug - Created 200+ evidence records
[TestMapping] 70+ capabilities now have evidence
```

### Ditt nuvarande scenario (behöver debug):
```
[CapabilityRegistry] Generated 51 capabilities
[TestMapping] Linked 201 tests to 36 capabilities
[TestMapping] 7 capabilities now have evidence  ← FÖR LÅGT
```

**201 tests är rätt** (70 tests × ~3 capabilities per test)  
**36 capabilities får tests är OK**  
**Bara 7 får evidence är PROBLEMET**

---

## 🔍 Möjliga problem

### Problem 1: RequiredEvidenceLevel
Capabilities kanske genereras med RequiredEvidenceLevel > ApiTests?

Check: Se `CapabilityGenerator.cs` line 82 - ska vara `ApiExecution` eller `ApiTests`

### Problem 2: Evidence registreras men MeetsEvidenceRequirement() bugg?

Check: Se `CapabilityRegistry.cs` `MeetsEvidenceRequirement()` method

### Problem 3: Capability IDs matchar inte
TestMappings har "login" men capabilities genereras som "auth-login"?

Check: Debug output kommer visa detta

---

## 📋 Nästa steg

1. **Starta om backend + MCP-server**
2. **Kopiera HELA debug-output**
3. **Dela output här** så jag kan se exakt vad som är fel
4. **Fixa den exakta orsaken**

**Se:** `DEBUG_EVIDENCE_ISSUE.md` för mer detaljer

---

**Kör nu och kopiera output!** 🔍
