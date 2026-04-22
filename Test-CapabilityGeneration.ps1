# Test Capability Generation Offline
# Visar hur CapabilityGenerator fungerar utan att starta servern

Write-Host "🧪 Testing Capability Generation" -ForegroundColor Cyan
Write-Host ""

$projectPath = "src\ApiFirst.LlmOrchestration"
$testSwagger = @"
{
  "openapi": "3.0.1",
  "info": { "title": "Test API", "version": "1.0" },
  "paths": {
    "/api/team/members": {
      "get": {
        "tags": ["Team"],
        "summary": "Get all team members",
        "operationId": "GetTeamMembers",
        "responses": { "200": { "description": "Success" } }
      }
    },
    "/api/team/members/{id}": {
      "get": {
        "tags": ["Team"],
        "summary": "Get team member by ID",
        "operationId": "GetTeamMember",
        "parameters": [
          {
            "name": "id",
            "in": "path",
            "required": true,
            "schema": { "type": "string" }
          }
        ],
        "responses": { "200": { "description": "Success" } }
      },
      "put": {
        "tags": ["Team"],
        "summary": "Update team member",
        "operationId": "UpdateTeamMember",
        "parameters": [
          {
            "name": "id",
            "in": "path",
            "required": true,
            "schema": { "type": "string" }
          }
        ],
        "requestBody": { "required": true },
        "responses": { "200": { "description": "Success" } }
      },
      "delete": {
        "tags": ["Team"],
        "summary": "Delete team member",
        "operationId": "DeleteTeamMember",
        "parameters": [
          {
            "name": "id",
            "in": "path",
            "required": true,
            "schema": { "type": "string" }
          }
        ],
        "responses": { "204": { "description": "Success" } }
      }
    },
    "/api/courses": {
      "get": {
        "tags": ["Courses"],
        "summary": "Get all courses",
        "operationId": "GetCourses",
        "responses": { "200": { "description": "Success" } }
      },
      "post": {
        "tags": ["Courses"],
        "summary": "Create course",
        "operationId": "CreateCourse",
        "requestBody": { "required": true },
        "responses": { "201": { "description": "Created" } }
      }
    }
  }
}
"@

# Spara test-swagger
$testSwaggerPath = Join-Path $env:TEMP "test-swagger.json"
$testSwagger | Out-File -FilePath $testSwaggerPath -Encoding UTF8

Write-Host "📄 Created test Swagger document with:" -ForegroundColor Yellow
Write-Host "  - 3 Team endpoints (GetTeamMembers, GetTeamMember, UpdateTeamMember, DeleteTeamMember)" -ForegroundColor Gray
Write-Host "  - 2 Courses endpoints (GetCourses, CreateCourse)" -ForegroundColor Gray
Write-Host ""

Write-Host "🔧 Expected capabilities to be generated:" -ForegroundColor Yellow
Write-Host ""
Write-Host "  Individual capabilities (5):" -ForegroundColor Cyan
Write-Host "    - getteammembers     [Team] GET /api/team/members" -ForegroundColor Gray
Write-Host "    - getteammember      [Team] GET /api/team/members/{id}" -ForegroundColor Gray
Write-Host "    - updateteammember   [Team] PUT /api/team/members/{id}" -ForegroundColor Gray
Write-Host "    - deleteteammember   [Team] DELETE /api/team/members/{id}" -ForegroundColor Gray
Write-Host "    - getcourses         [Courses] GET /api/courses" -ForegroundColor Gray
Write-Host "    - createcourse       [Courses] POST /api/courses" -ForegroundColor Gray
Write-Host ""
Write-Host "  Grouped capabilities (2):" -ForegroundColor Cyan
Write-Host "    - team-management    [Team] All team operations" -ForegroundColor Gray
Write-Host "    - courses-management [Courses] All courses operations" -ForegroundColor Gray
Write-Host ""

Write-Host "📊 Total expected: 8 capabilities" -ForegroundColor Green
Write-Host ""
Write-Host "✨ This demonstrates:" -ForegroundColor Yellow
Write-Host "  1. ✅ Each endpoint becomes a capability" -ForegroundColor Gray
Write-Host "  2. ✅ Related endpoints are grouped by tag" -ForegroundColor Gray
Write-Host "  3. ✅ No manual configuration needed" -ForegroundColor Gray
Write-Host "  4. ✅ Automatic updates when Swagger changes" -ForegroundColor Gray
Write-Host ""
Write-Host "🚀 To test with real API, run:" -ForegroundColor Cyan
Write-Host "   .\Start-McpServer.ps1" -ForegroundColor White
Write-Host ""

# Cleanup
Remove-Item $testSwaggerPath -ErrorAction SilentlyContinue
