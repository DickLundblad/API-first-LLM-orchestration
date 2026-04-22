# Quick Guide: Map Your Python Tests

## Your Test Example

File: `test/test_team_api.py`
```python
def test_admin_sees_all_members(self, client, setup_data):
    """Admin should see all team members."""
    # Login as admin
    client.post('/api/auth/login', json={'username': 'admin', 'password': 'Admin1234!'})

    response = client.get('/api/team', query_string={'includeInactive': 'true'})
```

## Step 1: Identify API Operations

Look at the HTTP calls in your test:
- `POST /api/auth/login` → **Login** operation
- `GET /api/team` → **GetTeamMembers** operation

## Step 2: Find Operation IDs in Swagger

Your Swagger spec defines operation IDs. Common patterns:
- `POST /api/auth/login` → `Login`
- `GET /api/team` → `GetTeamMembers`
- `GET /api/team/{id}` → `GetTeamMember`
- `PUT /api/team/{id}` → `UpdateTeamMember`

## Step 3: Create TestMappings.json

```json
{
  "tests": [
    {
      "testId": "test/test_team_api.py::test_admin_sees_all_members",
      "testName": "Admin sees all members",
      "operations": ["Login", "GetTeamMembers"],
      "capabilities": ["login", "getteammembers"]
    }
  ]
}
```

## Complete Example for Your Tests

```json
{
  "tests": [
    {
      "testId": "test/test_team_api.py::test_admin_sees_all_members",
      "testName": "Admin sees all team members",
      "operations": ["Login", "GetTeamMembers"],
      "capabilities": ["login", "getteammembers", "team-management"]
    },
    {
      "testId": "test/test_team_api.py::test_get_team_member_by_id",
      "operations": ["GetTeamMember"],
      "capabilities": ["getteammember"]
    },
    {
      "testId": "test/test_team_api.py::test_update_team_member",
      "operations": ["UpdateTeamMember"],
      "capabilities": ["updateteammember"]
    }
  ]
}
```

## Quick Start

1. Copy example:
```powershell
Copy-Item src\ApiFirst.LlmOrchestration.McpServer\TestMappings.example.json `
          src\ApiFirst.LlmOrchestration.McpServer\TestMappings.json
```

2. Edit the file:
```powershell
notepad src\ApiFirst.LlmOrchestration.McpServer\TestMappings.json
```

3. Add your tests like the example above

4. Start server:
```powershell
.\Start-McpServer.ps1
```

## Tips

**Test ID Format:**
Use pytest node ID format:
```
test/test_team_api.py::test_admin_sees_all_members
test/test_courses.py::TestCourseAPI::test_create_course
```

**One test can cover multiple operations:**
If your test calls multiple endpoints, list all operations:
```json
{
  "operations": ["Login", "GetTeamMembers", "GetCourses"]
}
```

**Link to multiple capabilities:**
```json
{
  "capabilities": ["login", "getteammembers", "team-management"]
}
```

See PYTHON_TEST_MAPPING.md for full details.
