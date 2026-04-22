# Dynamic Capability Generation from Swagger

## Översikt

MCP-servern genererar nu capabilities **dynamiskt från Swagger-endpoints** istället för att ladda från statisk JSON.

### Vad har ändrats?

#### ✅ Före (statisk)
- Laddade `CapabilityRegistry.json` med manuellt konfigurerade use cases
- Capabilities var förkonfigurerade och statiska
- Krävde manuell synkning mellan API och registry

#### ✅ Efter (dynamisk)
- Genererar capabilities direkt från Swagger-dokument vid start
- Varje endpoint blir automatiskt en capability
- Grupperar relaterade operationer heuristiskt
- Capabilities uppdateras automatiskt när API:et ändras

## Arkitektur

### CapabilityGenerator

Huvudklass för att generera capabilities från Swagger:

```csharp
// Generera från Swagger catalog
var capabilities = CapabilityGenerator.GenerateFromSwagger(catalog);

// Länka tester heuristiskt
CapabilityGenerator.LinkTestsHeuristically(registry, testNames);

// Beräkna coverage
var coverage = CapabilityGenerator.CalculateCoverage(registry);
```

### Generering

Varje Swagger-operation blir:

1. **En individuell capability** (finkorning)
   - ID: `operationId` (lowercase)
   - Namn: Operation summary
   - Beskrivning: HTTP method + path
   - Kategori: Första tag eller "General"

2. **En grupperad capability** (om flera operationer för samma resurs)
   - ID: `{resource}-management`
   - Samlar alla operationer för en resurs
   - Exempel: `team-management` för alla team-endpoints

### Heuristik

**Resource-gruppering:**
- Använder först Swagger tag
- Fallback: extraherar från path (`/api/team/members` → "team")
- Skippar `api`, `{id}` etc.

**Status-inferens:**
- GET/HEAD → `Planned` (säkra, troligen stabila)
- POST/PUT/DELETE → `Planned` (kräver mer validering)

**Test-länkning:**
- Matchar test-namn mot operation IDs
- Exempel: `GetTeamMemberTest` länkas till `GetTeamMember`

## Starta servern

### Lokal Swagger URL

```powershell
cd src\ApiFirst.LlmOrchestration.McpServer
dotnet run -- --swagger-url http://localhost:5000/api/swagger.json --api-base-url http://localhost:5000
```

### HTTP-läge (för enkel testning)

```powershell
dotnet run -- --http-prefix http://localhost:5055
```

### Med appsettings.json

Servern läser automatiskt från `appsettings.json`:

```json
{
  "McpServer": {
    "DefaultSwaggerUrl": "http://localhost:5000/api/swagger.json",
    "DefaultApiBaseUrl": "http://localhost:5000"
  }
}
```

Kör bara:
```powershell
dotnet run
```

## MCP Verktyg

### Nya verktyg

#### `capability_coverage`
Visar test coverage för alla capabilities:

```json
{
  "totalCapabilities": 24,
  "capabilitiesWithTests": 8,
  "capabilitiesWithoutTests": 16,
  "totalOperations": 24,
  "totalTests": 12,
  "overallCoveragePercentage": 33.3,
  "details": [...]
}
```

### Befintliga verktyg

- `list_capabilities` - Lista alla (nu genererade) capabilities
- `get_capability` - Hämta detaljer för specifik capability
- `validate_capability` - Validera mot faktiskt API
- `capability_health` - Hälsorapport

## Exempel: Använd från LLM-klient

### 1. Lista alla genererade capabilities

```
"Lista alla capabilities"
→ Anropar list_capabilities
→ Visar alla endpoints som capabilities
```

### 2. Se coverage

```
"Visa test coverage för capabilities"
→ Anropar capability_coverage
→ Visar vilka som har tester, vilka som saknar
```

### 3. Validera en capability

```
"Validera team-detail capability"
→ Anropar validate_capability
→ Kör faktiska API-anrop (GET/HEAD som standard)
```

## Utvidgning

### Länka tester manuellt

```csharp
var testNames = new List<string> 
{ 
    "GetTeamMemberTest", 
    "UpdateTeamMemberTest" 
};

CapabilityGenerator.LinkTestsHeuristically(registry, testNames);
```

### Anpassad coverage-analys

```csharp
var report = CapabilityGenerator.CalculateCoverage(registry);

foreach (var detail in report.Details.Where(d => !d.HasTests))
{
    Console.WriteLine($"Saknar test: {detail.Name} ({detail.Category})");
}
```

## Filosofi

### API-first principer

1. **Swagger är källan till sanning** - Inte JSON-filer
2. **Coverage över perfekt mapping** - Visa vad vi vet, inte vad vi påstår
3. **Heuristik över manuell konfiguration** - Automatisera där det går
4. **Use cases byggs ovanpå** - Capabilities är byggstenar, inte färdiga lösningar

### Vad detta löser

- ✅ Ingen manuell synkning mellan API och registry
- ✅ Capabilities uppdateras automatiskt när Swagger ändras
- ✅ Transparent coverage-rapportering
- ✅ Enklare att se vad som faktiskt finns vs. vad som testas
- ✅ Snabbare att komma igång

### Nästa steg

Capabilities är nu atomiska byggstenar. Bygg use cases ovanpå:

1. **Capability = 1 endpoint** (eller grupp av relaterade)
2. **Use case = kedja av capabilities** (kommer senare)
3. **Test coverage = vad vi faktiskt vet**
4. **Runtime validation = kontinuerlig verifiering**

## Felsökning

### Inga capabilities genereras

Kontrollera:
- Är Swagger URL korrekt?
- Svarar API:et?
- Finns det operations i Swagger-dokumentet?

Kolla loggen:
```
[CapabilityRegistry] Generated X capabilities from Swagger
```

### Coverage visar 0%

- Har du länkat tester?
- Använd `LinkTestsHeuristically` för att matcha test-namn

### Capabilities grupperas fel

- Swagger tags styr gruppering
- Fallback: path-extraktion
- Anpassa heuristiken i `GetResourceName()` om nödvändigt
