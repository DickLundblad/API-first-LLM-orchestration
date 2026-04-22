# Länka Python-tester till Capabilities

## Översikt

Din Python test-suite i ett annat repo kan länkas till capabilities i detta repo via **TestMappings.json**.

## Metod 1: Manuell konfiguration (Rekommenderat)

### Steg 1: Generera template

Kör MCP-servern och använd verktyget:

```
"Generera test mapping template"
→ Anropar generate_test_template verktyget
→ Skapar TestMappings.json med alla capabilities
```

Eller manuellt via LLM:
```json
{
  "name": "generate_test_template",
  "arguments": {
    "outputPath": "TestMappings.json"
  }
}
```

### Steg 2: Redigera TestMappings.json

Filen genereras med alla capabilities. Lägg till dina Python-tester:

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
      "testId": "tests/api/test_auth.py::test_login_success",
      "testName": "Test Login Success",
      "operations": ["Login"],
      "capabilities": ["login"]
    }
  ]
}
```

**Fält:**
- `testId`: Unik identifierare för testet (pytest node ID fungerar bra)
- `testName`: Läsbart namn (valfritt)
- `operations`: Lista över API operation IDs som testet täcker
- `capabilities`: Lista över capability IDs som testet validerar

### Steg 3: Placera filen

Lägg `TestMappings.json` i:
- `src/ApiFirst.LlmOrchestration.McpServer/TestMappings.json` (rekommenderat)
- Eller workspace root

### Steg 4: Starta om MCP-servern

Servern läser filen vid start:

```
[TestMapping] Loaded test mappings from TestMappings.json
[TestMapping] Linked 12 tests to 8 capabilities
```

## Metod 2: Från pytest JSON-rapport

### Steg 1: Generera pytest rapport

I ditt Python test-repo:

```bash
pytest --json-report --json-report-file=pytest-report.json
```

### Steg 2: Kopiera rapporten

Kopiera `pytest-report.json` till:
- `src/ApiFirst.LlmOrchestration.McpServer/pytest-report.json`

### Steg 3: Starta MCP-servern

Servern läser rapporten och **infererar mappningar heuristiskt**:

```python
# test_get_team_member → GetTeamMember
# test_update_course → UpdateCourse
```

**OBS:** Heuristiken är inte perfekt. Använd manuell mapping för precision.

## Test ID Format

### Rekommenderat format (pytest)

```
tests/api/test_team.py::TestTeamAPI::test_get_member_success
tests/integration/test_workflow.py::test_course_enrollment_e2e
```

### Alternativa format

Du kan använda vilket format som helst:
```
team.get_member.test
api-test-1234
GetTeamMember_ValidationTest
```

## Exempel: Komplett mappning

```json
{
  "tests": [
    {
      "testId": "tests/api/test_team.py::test_get_team_members",
      "testName": "Get all team members",
      "operations": ["GetTeamMembers"],
      "capabilities": ["getteammembers", "team-management"]
    },
    {
      "testId": "tests/api/test_team.py::test_get_team_member[valid-id]",
      "testName": "Get team member with valid ID",
      "operations": ["GetTeamMember"],
      "capabilities": ["getteammember"]
    },
    {
      "testId": "tests/api/test_team.py::test_get_team_member[invalid-id]",
      "testName": "Get team member with invalid ID (error case)",
      "operations": ["GetTeamMember"],
      "capabilities": ["getteammember"]
    },
    {
      "testId": "tests/api/test_team.py::test_update_team_member",
      "operations": ["UpdateTeamMember"],
      "capabilities": ["updateteammember", "team-management"]
    },
    {
      "testId": "tests/api/test_courses.py::test_enroll_course_success",
      "operations": ["EnrollCourse", "GetConsultantCourses"],
      "capabilities": ["enrollcourse", "getconsultantcourses"]
    },
    {
      "testId": "tests/e2e/test_course_enrollment_workflow.py::test_complete_enrollment",
      "testName": "Complete course enrollment workflow (E2E)",
      "operations": [
        "Login",
        "GetCourses",
        "GetTeamMember",
        "EnrollCourse",
        "GetConsultantCourses"
      ],
      "capabilities": ["course-enrollment-workflow"]
    }
  ]
}
```

## Verifiera mappningar

### Via MCP-verktyg

```
"Visa test coverage"
→ capability_coverage verktyget
→ Visar vilka capabilities som har tester
```

### Via LLM

```
"Vilka tester finns för getteammember capability?"
→ get_capability verktyget
→ Visar alla länkade tester
```

## Uppdatera mappningar

### Automatisk reload

Starta om MCP-servern för att ladda ändringar.

### Dynamisk uppdatering (framtida)

Planerat: Lägg till verktyg för att uppdatera mappningar runtime.

## Best Practices

### 1. Använd operation IDs när möjligt

```json
{
  "operations": ["GetTeamMember", "UpdateTeamMember"]
}
```

Servern länkar automatiskt till rätt capabilities.

### 2. Ett test kan täcka flera capabilities

```json
{
  "testId": "tests/e2e/test_workflow.py::test_enrollment",
  "operations": ["Login", "GetCourses", "EnrollCourse"],
  "capabilities": ["login", "getcourses", "enrollcourse", "enrollment-workflow"]
}
```

### 3. Använd pytest node IDs

```
tests/api/test_team.py::TestTeamAPI::test_get_member
```

Gör det enkelt att hitta och köra testet.

### 4. Dokumentera komplexitet

```json
{
  "testId": "tests/integration/test_auth_flow.py::test_full_auth",
  "testName": "Complete authentication flow with cookie management",
  "operations": ["Login", "GetTeamMembers"],
  "capabilities": ["login", "session-management"]
}
```

## Integration med CI/CD

### GitHub Actions exempel

```yaml
name: Update Test Mappings

on:
  push:
    branches: [main]
    paths:
      - 'tests/**'

jobs:
  update-mappings:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Run Python tests with JSON report
        run: |
          pytest --json-report --json-report-file=pytest-report.json

      - name: Copy report to MCP server
        run: |
          cp pytest-report.json ../API-first-LLM-orchestration/src/ApiFirst.LlmOrchestration.McpServer/

      - name: Commit updated report
        run: |
          git add pytest-report.json
          git commit -m "Update pytest report"
          git push
```

## Felsökning

### Inga tester länkas

Kontrollera:
1. Finns `TestMappings.json` i rätt mapp?
2. Är JSON-syntaxen korrekt?
3. Matchar `operations` faktiska operation IDs från Swagger?
4. Matchar `capabilities` genererade capability IDs?

### Fel capability IDs

Capabilities genereras med lowercase operation IDs:
- `GetTeamMember` → capability ID: `getteammember`
- `UpdateCourse` → capability ID: `updatecourse`

Lista alla med:
```
"Lista alla capabilities"
→ Visar alla IDs
```

### Test räknas inte

Ett test räknas endast om:
1. Det finns i `TestMappings.json` ELLER `pytest-report.json`
2. `operations` eller `capabilities` matchar existerande capabilities

## Se också

- `TestMappings.example.json` - Exempel-mappningar
- `CAPABILITY_GENERATION.md` - Hur capabilities genereras
- MCP-verktyg: `generate_test_template`, `capability_coverage`, `get_capability`
