# ✅ KOMPLETT: Python Test Integration

## Vad du kan göra nu

### 1️⃣ **Generera capabilities från Swagger**
```powershell
.\Start-McpServer.ps1
```
✅ Alla endpoints blir automatiskt capabilities  
✅ Grupperas per resurs/tag  
✅ Inga manuella konfigfiler behövs

---

### 2️⃣ **Länka Python-tester till capabilities**

#### Metod A: Manuell konfiguration (Rekommenderat)

**Steg 1:** Skapa TestMappings.json
```powershell
# Kopiera exempel
Copy-Item src\ApiFirst.LlmOrchestration.McpServer\TestMappings.example.json `
          src\ApiFirst.LlmOrchestration.McpServer\TestMappings.json
```

**Steg 2:** Redigera filen
```json
{
  "tests": [
    {
      "testId": "tests/api/test_team.py::test_get_team_member",
      "testName": "Test Get Team Member",
      "operations": ["GetTeamMember"],
      "capabilities": ["getteammember", "team-management"]
    },
    {
      "testId": "tests/api/test_courses.py::test_enroll_course",
      "operations": ["EnrollCourse"],
      "capabilities": ["enrollcourse"]
    }
  ]
}
```

**Steg 3:** Starta servern
```powershell
.\Start-McpServer.ps1

# Output:
# [CapabilityRegistry] Generated 24 capabilities from Swagger
# [TestMapping] Loaded test mappings from TestMappings.json
# [TestMapping] Linked 12 tests to 8 capabilities
```

#### Metod B: Från pytest-rapport (Automatisk inferens)

**I ditt Python test-repo:**
```bash
pytest --json-report --json-report-file=pytest-report.json
```

**Kopiera till MCP-server:**
```powershell
Copy-Item ..\python-tests\pytest-report.json `
          src\ApiFirst.LlmOrchestration.McpServer\
```

**Starta servern** - mappningar infereras automatiskt!

---

### 3️⃣ **Visa test coverage**

Via MCP-verktyg:
```
"Visa test coverage"
→ capability_coverage verktyget
→ Visar vilka capabilities som har tester
```

Output:
```json
{
  "totalCapabilities": 24,
  "capabilitiesWithTests": 8,
  "overallCoveragePercentage": 33.3,
  "details": [
    {
      "capabilityId": "getteammember",
      "hasTests": true,
      "testCount": 2,
      "coveragePercentage": 100.0
    }
  ]
}
```

---

### 4️⃣ **Generera test-template**

Via MCP-verktyg:
```
"Generera test mapping template"
→ generate_test_template verktyget
→ Skapar TestMappings.json med alla capabilities
```

---

## 📁 Filer

### Skapade
- ✅ `src\ApiFirst.LlmOrchestration\Registry\CapabilityGenerator.cs` - Genererar från Swagger
- ✅ `src\ApiFirst.LlmOrchestration\Registry\ExternalTestMapper.cs` - Länkar Python-tester
- ✅ `src\ApiFirst.LlmOrchestration\Registry\TestDiscoveryService.cs` - .NET test discovery (om behövs)
- ✅ `src\ApiFirst.LlmOrchestration.McpServer\TestMappings.example.json` - Exempel-mappningar
- ✅ `src\ApiFirst.LlmOrchestration.McpServer\PYTHON_TEST_MAPPING.md` - Komplett guide
- ✅ `Guide-PythonTests.ps1` - Interaktiv guide

### Modifierade
- ✅ `src\ApiFirst.LlmOrchestration.McpServer\McpServer.cs` - Dynamisk generering + test-länkning

---

## 🎯 Arbetsflöde

```
1. Python Repo                    2. Detta Repo
   ├── tests/                        ├── Swagger endpoint
   │   ├── test_team.py              ↓
   │   └── test_courses.py           Capabilities genereras
   └── pytest-report.json            ↓
        ↓                            TestMappings.json
        Kopieras                     ↓
        ↓                            Tester länkas
        ↓                            ↓
        └─────────────────────────→ MCP Server
                                     ↓
                                     Coverage-rapport
```

---

## 🚀 Snabbstart

```powershell
# 1. Se guide för Python-tester
.\Guide-PythonTests.ps1

# 2. Skapa test-mappningar
Copy-Item src\ApiFirst.LlmOrchestration.McpServer\TestMappings.example.json `
          src\ApiFirst.LlmOrchestration.McpServer\TestMappings.json

# Redigera filen med dina Python-tester
notepad src\ApiFirst.LlmOrchestration.McpServer\TestMappings.json

# 3. Starta MCP-servern
.\Start-McpServer.ps1

# 4. Använd från LLM-klient
# "Visa test coverage"
# "Visa capability getteammember"
# "Validera team-detail"
```

---

## 📊 Exempel

### Före
- ❌ Statisk CapabilityRegistry.json
- ❌ Manuell synkning mellan API och registry
- ❌ Ingen test-länkning
- ❌ Oklar coverage

### Efter
- ✅ Capabilities från Swagger (automatiskt)
- ✅ Python-tester länkade via TestMappings.json
- ✅ Coverage-rapportering (transparent)
- ✅ Runtime validation

---

## 🔧 MCP-verktyg

| Verktyg | Beskrivning |
|---------|-------------|
| `list_capabilities` | Lista alla genererade capabilities |
| `get_capability` | Detaljer + länkade tester för specifik capability |
| `capability_coverage` | Test coverage-rapport |
| `generate_test_template` | Generera TestMappings.json template |
| `validate_capability` | Runtime API-validering |
| `capability_health` | Hälsorapport |

---

## 📚 Dokumentation

1. **`CAPABILITY_GENERATION_SUMMARY.md`** - Översikt av allt
2. **`src\ApiFirst.LlmOrchestration.McpServer\CAPABILITY_GENERATION.md`** - Detaljerad capability-generering
3. **`src\ApiFirst.LlmOrchestration.McpServer\PYTHON_TEST_MAPPING.md`** - Komplett Python test guide
4. **`Guide-PythonTests.ps1`** - Interaktiv snabbguide

---

## ✨ Nästa steg

Nu när capabilities är dynamiska och tester länkade:

1. **Komponera use cases** - Kedjor av capabilities
2. **CI/CD integration** - Automatiska test-rapporter
3. **Runtime validation** - Kontinuerlig API-verifiering
4. **Coverage tracking** - Följ test-utveckling över tid

---

## 🎉 Klart!

Du har nu:
- ✅ Dynamisk capability-generering från Swagger
- ✅ Python test-integration via TestMappings.json
- ✅ Transparent coverage-rapportering
- ✅ MCP-verktyg för LLM-agenter
- ✅ API-first arkitektur

**Kör:** `.\Start-McpServer.ps1` för att testa! 🚀
