# 🔴 PROBLEM IDENTIFIERAT: Swagger saknar operationIds

## Vad som hände

### Swagger-status:
- ✅ 37 endpoints (paths)
- ❌ **Bara 1 operationId** (login)
- ❌ 36 endpoints saknar operationId

### Exempel från din Swagger:
```json
{
  "/auth/login": {
    "post": {
      "operationId": "login",  ✅
      ...
    }
  },
  "/team": {
    "get": {
      // ❌ SAKNAR operationId!
    }
  }
}
```

### Resultat:
- CapabilityGenerator skapar 51 capabilities (från paths)
- ExternalTestMapper kan bara länka tester via operationId
- Bara 1 operationId finns → bara 3 tester länkas

---

## LÖSNINGAR

### Option 1: Fixa Swagger (REKOMMENDERAT)

Din InternalAI backend behöver lägga till operationIds i Swagger-specen.

**I Python Flask/FastAPI:**
```python
@app.route('/api/team', methods=['GET'])
def get_team_members():
    """
    Get all team members
    ---
    operationId: GetTeamMembers  ← LÄGG TILL DETTA!
    """
    pass
```

**eller med Flasgger:**
```python
@app.route('/api/team')
@swag_from({
    'operationId': 'GetTeamMembers',
    'responses': {200: {...}}
})
def get_team_members():
    pass
```

### Option 2: Mappa via Path (QUICK FIX)

Jag kan uppdatera ExternalTestMapper att matcha paths också:

```csharp
// Matcha både operationId OCH path
if (operation.Path == "/api/team" && operation.Method == "GET") {
    // Match till GetTeamMembers test
}
```

### Option 3: Manuell Capability-konfiguration

Skippa Swagger-generering och definiera capabilities manuellt.

---

## REKOMMENDERAD ACTION

### Steg 1: Lägg till operationIds i din Python backend

**Fil:** `C:\git\InternalAI\backend\routes\team.py` (eller motsvarande)

```python
from flask import Blueprint
from flasgger import swag_from

team_bp = Blueprint('team', __name__)

@team_bp.route('/api/team', methods=['GET'])
@swag_from({
    'operationId': 'GetTeamMembers',  ← LÄGG TILL
    'summary': 'Get all team members',
    'responses': {
        200: {'description': 'Success'}
    }
})
def get_team_members():
    # ...
```

### Steg 2: Verifiera Swagger

```bash
# I Python backend-repot
python app.py

# Kolla Swagger UI
# http://localhost:5000/apidocs

# Kolla JSON
curl http://localhost:5000/api/swagger.json | jq '.paths'
```

### Steg 3: Starta om MCP-servern

Efter att Swagger är fixad:
```powershell
.\Start-McpServer.ps1
```

Nu ska alla 70 tester länkas korrekt!

---

## QUICK FIX (om du inte kan ändra backend nu)

Jag kan göra mappningen smartare så den matchar på path också.

Vill du att jag:
1. ✅ Uppdaterar ExternalTestMapper att också matcha paths?
2. ✅ Skapar en path→operation mapping?

Då kan vi få fler tester länkade även med ofullständig Swagger.

---

## Varför bara 3 av 70 länkades

TestMappings.json har:
```json
{
  "operations": ["Login", "GetTeamMembers"],
  "capabilities": ["login", "getteammembers"]
}
```

ExternalTestMapper försöker:
1. Hitta capability med ID "login" ✅ (finns)
2. Hitta capability med ID "getteammembers" ❓
3. Hitta capabilities för operation "Login" ✅ (finns i Swagger)
4. Hitta capabilities för operation "GetTeamMembers" ❌ (SAKNAS i Swagger!)

Eftersom Swagger bara har 1 operation (Login), kan bara tester med "Login" i operations länkas via operation-mappning.

Direct capability ID matching fungerar för de 3 som har exakt rätt ID.

---

## NÄSTA STEG

**VAL 1: Fixa Swagger (bäst långsiktigt)**
- Lägg till operationIds i Python backend
- Starta om backend
- Starta om MCP-server
- ✅ Alla 70 tester länkas automatiskt

**VAL 2: Quick fix (fungerar nu)**
- Jag uppdaterar ExternalTestMapper
- Lägger till path-based matching
- ✅ Fler tester länkas utan att ändra backend

**Vad föredrar du?**
