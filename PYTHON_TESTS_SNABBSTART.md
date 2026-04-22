# 🚀 Snabbstart: Länka Python-tester

## Vad du ska göra

Din Python test-suite (i annat repo) ska länkas till capabilities i detta repo.

## Steg-för-steg

### 1. Kör guiden
```powershell
.\Guide-PythonTests.ps1
```

### 2. Skapa TestMappings.json
```powershell
# Kopiera exempel
Copy-Item src\ApiFirst.LlmOrchestration.McpServer\TestMappings.example.json `
          src\ApiFirst.LlmOrchestration.McpServer\TestMappings.json

# Redigera
notepad src\ApiFirst.LlmOrchestration.McpServer\TestMappings.json
```

### 3. Lägg till dina Python-tester

```json
{
  "tests": [
    {
      "testId": "tests/api/test_team.py::test_get_team_member",
      "operations": ["GetTeamMember"],
      "capabilities": ["getteammember"]
    }
  ]
}
```

**testId**: Ditt pytest node ID  
**operations**: Swagger operation IDs som testet täcker  
**capabilities**: Capability IDs (auto-genereras från operations)

### 4. Starta MCP-servern
```powershell
.\Start-McpServer.ps1
```

Se output:
```
[TestMapping] Loaded test mappings from TestMappings.json
[TestMapping] Linked 12 tests to 8 capabilities
```

### 5. Verifiera coverage
Via LLM-klient:
```
"Visa test coverage"
→ Visar vilka capabilities som har tester
```

## 📚 Mer info

- `PYTHON_TEST_INTEGRATION.md` - Komplett guide
- `src\ApiFirst.LlmOrchestration.McpServer\PYTHON_TEST_MAPPING.md` - Detaljerad dokumentation
- `Guide-PythonTests.ps1` - Interaktiv guide

## 💡 Tips

### Hitta operation IDs
Din Swagger-spec definierar operation IDs. Exempel:
- `GetTeamMember`
- `UpdateTeamMember`
- `EnrollCourse`

### Capability IDs
Genereras automatiskt (lowercase):
- `GetTeamMember` → `getteammember`
- `EnrollCourse` → `enrollcourse`

Lista alla: `"Lista alla capabilities"` via LLM

### Pytest format
Använd pytest node IDs:
```
tests/api/test_team.py::test_get_member
tests/integration/test_workflow.py::TestEnrollment::test_success
```

## ⚡ Alternativ: Automatisk inferens

Från ditt Python-repo:
```bash
pytest --json-report --json-report-file=pytest-report.json
```

Kopiera till MCP-server:
```powershell
Copy-Item pytest-report.json src\ApiFirst.LlmOrchestration.McpServer\
```

Starta servern - mappningar infereras automatiskt!

---

**Klar? Kör:** `.\Start-McpServer.ps1` 🎉
