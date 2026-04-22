# Finding Operation IDs from Your Tests

## Pattern: HTTP Endpoint → Operation ID

Based on typical Swagger/OpenAPI conventions:

| HTTP Call in Test | Likely Operation ID |
|-------------------|---------------------|
| `POST /api/auth/login` | `Login` |
| `GET /api/team` | `GetTeamMembers` |
| `GET /api/team?filter=all` | `GetTeamMembers` |
| `GET /api/team/{id}` | `GetTeamMember` |
| `PUT /api/team/{id}` | `UpdateTeamMember` |
| `DELETE /api/team/{id}` | `DeleteTeamMember` |
| `GET /api/courses` | `GetCourses` |
| `GET /api/courses/{id}` | `GetCourse` |
| `POST /api/courses` | `CreateCourse` |
| `PUT /api/courses/{id}` | `UpdateCourse` |
| `POST /api/courses/{id}/approve` | `ApproveCourse` |
| `POST /api/team/{id}/courses` | `EnrollCourse` |
| `GET /api/team/{id}/courses` | `GetConsultantCourses` |

## How to Verify

### Option 1: Start MCP Server and Ask

```powershell
.\Start-McpServer.ps1
```

Then from your LLM client:
```
"List all API operations"
```

This will show all operation IDs from your Swagger.

### Option 2: Check Your Swagger/OpenAPI File

Look for `operationId` fields:

```json
{
  "paths": {
    "/api/team": {
      "get": {
        "operationId": "GetTeamMembers",
        ...
      }
    }
  }
}
```

### Option 3: Common Patterns

If your API follows REST conventions:

**Collection endpoints:**
- `GET /api/resource` → `GetResources` or `ListResources`
- `POST /api/resource` → `CreateResource`

**Single resource:**
- `GET /api/resource/{id}` → `GetResource`
- `PUT /api/resource/{id}` → `UpdateResource`
- `DELETE /api/resource/{id}` → `DeleteResource`

**Actions:**
- `POST /api/resource/{id}/action` → `ActionResource`
- Example: `POST /api/courses/{id}/approve` → `ApproveCourse`

## Your Test Example Mapping

```python
# test/test_team_api.py
def test_admin_sees_all_members(self, client, setup_data):
    # POST /api/auth/login → Login
    client.post('/api/auth/login', json={'username': 'admin', 'password': 'Admin1234!'})

    # GET /api/team → GetTeamMembers
    response = client.get('/api/team', query_string={'includeInactive': 'true'})
```

Maps to:

```json
{
  "testId": "test/test_team_api.py::test_admin_sees_all_members",
  "operations": ["Login", "GetTeamMembers"]
}
```

## Capability IDs (Auto-Generated)

Operation IDs become capability IDs (lowercase):

| Operation ID | Capability ID |
|--------------|---------------|
| `Login` | `login` |
| `GetTeamMembers` | `getteammembers` |
| `GetTeamMember` | `getteammember` |
| `UpdateTeamMember` | `updateteammember` |
| `EnrollCourse` | `enrollcourse` |
| `GetConsultantCourses` | `getconsultantcourses` |

Plus grouped capabilities:
- `team-management` (all team operations)
- `courses-management` (all course operations)

## Quick Reference for Your TestMappings.json

```json
{
  "tests": [
    {
      "testId": "test/test_team_api.py::test_admin_sees_all_members",
      "operations": ["Login", "GetTeamMembers"],
      "capabilities": ["login", "getteammembers"]
    },
    {
      "testId": "test/test_team_api.py::test_get_member_details",
      "operations": ["GetTeamMember"],
      "capabilities": ["getteammember"]
    },
    {
      "testId": "test/test_team_api.py::test_update_member_info",
      "operations": ["UpdateTeamMember"],
      "capabilities": ["updateteammember"]
    },
    {
      "testId": "test/test_courses.py::test_enroll_course",
      "operations": ["EnrollCourse", "GetConsultantCourses"],
      "capabilities": ["enrollcourse", "getconsultantcourses"]
    }
  ]
}
```
