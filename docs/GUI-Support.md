# GUI Support Integration

The MCP Server now includes GUI support metadata to help clients understand which API operations have corresponding user interface features.

## Overview

When working with APIs that have a separate frontend (like the InternalAI JavaScript GUI), the MCP server can provide information about which operations can be performed through the GUI, helping users choose between programmatic API access and the visual interface.

## Configuration

GUI support is configured through the `GuiSupportMappings.json` file located in the MCP server directory.

### GuiSupportMappings.json Structure

```json
{
  "guiBaseUrl": "http://localhost:3000",
  "mappings": [
    {
      "operationId": "GetTeamMembers",
      "guiRoute": "/team",
      "guiFeature": "Team Member List",
      "description": "View and search all team members in a table view"
    },
    {
      "operationId": "GetTeamMember",
      "guiRoute": "/team/{id}",
      "guiFeature": "Team Member Detail",
      "description": "View and edit individual team member profile details",
      "pathParameters": {
        "id": "memberId"
      },
      "notes": "Optional notes about this mapping"
    }
  ],
  "notes": [
    "GUI routes are relative to guiBaseUrl",
    "Path parameters in curly braces {id} should be replaced with actual values"
  ]
}
```

### Mapping Fields

- **operationId**: The OpenAPI operation ID from the Swagger document
- **guiRoute**: The frontend route (relative to guiBaseUrl) where this feature is available
- **guiFeature**: Human-readable name of the GUI feature
- **description**: Description of what the GUI feature does
- **pathParameters** (optional): Maps path template variables to API parameter names
  - Example: `{id}` in route maps to `memberId` parameter
- **notes** (optional): Additional information about the mapping

## How It Works

1. **Automatic Loading**: The MCP server automatically loads `GuiSupportMappings.json` on startup
2. **Operation Enrichment**: When listing or searching operations, GUI support info is included
3. **Graceful Degradation**: If the file is missing or invalid, the server continues without GUI support

## Using GUI Support Information

### In list_operations Response

```json
{
  "result": {
    "content": [...],
    "structuredContent": {
      "guiBaseUrl": "http://localhost:3000",
      "operations": [
        {
          "operationId": "GetTeamMembers",
          "method": "GET",
          "path": "/api/team",
          "summary": "Get team members",
          "hasGuiSupport": true,
          "guiRoute": "/team",
          "guiFeature": "Team Member List",
          "guiDescription": "View and search all team members in a table view"
        }
      ]
    }
  }
}
```

### In search_operations Response

```json
{
  "result": {
    "structuredContent": {
      "matches": [
        {
          "operationId": "UpdateTeamMember",
          "method": "PATCH",
          "path": "/api/team/{id}",
          "hasGuiSupport": true,
          "guiRoute": "/team/{id}",
          "guiFeature": "Team Member Edit"
        }
      ]
    }
  }
}
```

## Maintaining Mappings

### For the InternalAI Project

The current mappings cover:

- **Team Management**: View, edit, delete team members
- **Course Management**: List, create, edit, approve, archive courses
- **Course Enrollment**: Enroll team members in courses
- **Authentication**: Login, password reset, email verification

### Adding New Mappings

When new GUI features are added to the frontend:

1. Identify the API operation ID from the Swagger document
2. Determine the GUI route where the feature is available
3. Add a new mapping to `GuiSupportMappings.json`
4. Restart the MCP server (or reload configuration if supported)

### Example Workflow

1. Frontend team adds new "Projects" page at `/projects`
2. Backend exposes `GetProjects` operation
3. Update `GuiSupportMappings.json`:

```json
{
  "operationId": "GetProjects",
  "guiRoute": "/projects",
  "guiFeature": "Projects List",
  "description": "View and manage all projects"
}
```

## Programmatic Access

The `GuiSupportProvider` class provides programmatic access to GUI mappings:

```csharp
var provider = new GuiSupportProvider();

// Check if operation has GUI support
bool hasGui = provider.HasGuiSupport("GetTeamMembers");

// Get GUI mapping
var mapping = provider.GetGuiSupport("GetTeamMembers");
Console.WriteLine($"GUI Route: {mapping.GuiRoute}");

// Construct full GUI URL with parameters
var url = provider.GetGuiUrl("GetTeamMember", new Dictionary<string, string>
{
    ["memberId"] = "123"
});
// Returns: "http://localhost:3000/team/123"
```

## Benefits

1. **Discovery**: Clients can discover which operations have GUI alternatives
2. **User Guidance**: Help users choose between API and GUI based on their needs
3. **Documentation**: Serves as living documentation of API-GUI relationships
4. **Integration**: Enables hybrid workflows combining API automation and GUI interaction

## Related Files

- `src/ApiFirst.LlmOrchestration.McpServer/GuiSupportMappings.json` - Configuration file
- `src/ApiFirst.LlmOrchestration/Models/GuiSupportMapping.cs` - Data models
- `src/ApiFirst.LlmOrchestration/Swagger/GuiSupportProvider.cs` - Service implementation
- `src/ApiFirst.LlmOrchestration.McpServer/McpServer.cs` - MCP integration
