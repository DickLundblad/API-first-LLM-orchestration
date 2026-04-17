# GUI Support Integration

The MCP Server now includes GUI support metadata to help clients understand which API operations have corresponding user interface features, with screenshots and the ability to preview GUI routes directly in your browser.

## Overview

When working with APIs that have a separate frontend (like the InternalAI JavaScript GUI), the MCP server can provide information about which operations can be performed through the GUI, helping users choose between programmatic API access and the visual interface.

## Features

### 1. **GUI Route Discovery**
Query which operations have GUI support and where they're located in the frontend.

### 2. **Screenshot & Thumbnail Support**
View visual previews of GUI features with full screenshots and thumbnails.

### 3. **Browser Preview**
Open GUI routes directly in your default browser with proper parameter substitution.

## Configuration

GUI support is configured through the `GuiSupportMappings.json` file located in the MCP server directory.

### GuiSupportMappings.json Structure

```json
{
  "guiBaseUrl": "http://localhost:3000",
  "screenshotBaseUrl": "https://raw.githubusercontent.com/DickLundblad/InternalAI/main/docs/screenshots",
  "mappings": [
    {
      "operationId": "GetTeamMembers",
      "guiRoute": "/team",
      "guiFeature": "Team Member List",
      "description": "View and search all team members in a table view",
      "screenshotUrl": "team-list.png",
      "thumbnailUrl": "team-list-thumb.png"
    },
    {
      "operationId": "GetTeamMember",
      "guiRoute": "/team/{id}",
      "guiFeature": "Team Member Detail",
      "description": "View and edit individual team member profile details",
      "pathParameters": {
        "id": "memberId"
      },
      "screenshotUrl": "team-detail.png",
      "thumbnailUrl": "team-detail-thumb.png",
      "notes": "Optional notes about this mapping"
    }
  ],
  "notes": [
    "GUI routes are relative to guiBaseUrl",
    "Screenshot URLs are relative to screenshotBaseUrl"
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
- **screenshotUrl** (optional): Relative path to a full-size screenshot (relative to screenshotBaseUrl)
- **thumbnailUrl** (optional): Relative path to a thumbnail image (relative to screenshotBaseUrl)
- **notes** (optional): Additional information about the mapping

### Screenshot Guidelines

- **Thumbnails**: Typically 300x200px, used in search results and listings
- **Screenshots**: Full resolution, used in detail views
- **Format**: PNG or JPEG recommended
- **Location**: Place in InternalAI repository under `docs/screenshots/`
- **Naming**: Use descriptive names matching the feature (e.g., `team-list.png`)

## MCP Tools

### preview_gui

Opens a GUI route in the default browser for operations with GUI support.

**Parameters:**
- `operationId` (required): The operation to preview
- `parameters` (optional): JSON object with parameter values for parameterized routes
- `openInBrowser` (optional): Whether to open in browser (default: true)

**Example:**
```json
{
  "operationId": "GetTeamMember",
  "parameters": { "memberId": "123" },
  "openInBrowser": true
}
```

**Response:**
```json
{
  "guiUrl": "http://localhost:3000/team/123",
  "guiFeature": "Team Member Detail",
  "openedInBrowser": true
}
```

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
          "guiDescription": "View and search all team members in a table view",
          "screenshotUrl": "https://raw.githubusercontent.com/.../team-list.png",
          "thumbnailUrl": "https://raw.githubusercontent.com/.../team-list-thumb.png"
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
          "guiFeature": "Team Member Edit",
          "thumbnailUrl": "https://raw.githubusercontent.com/.../team-detail-thumb.png"
        }
      ]
    }
  }
}
```

## How It Works

1. **Startup**: MCP server loads `GuiSupportMappings.json`
2. **Query**: When client calls `list_operations`, `search_operations`, or `preview_gui`
3. **Enrich**: Each operation is checked against GUI mappings
4. **Response**: Results include GUI support flags, routes, descriptions, and screenshot URLs
5. **Preview**: `preview_gui` constructs full URLs and optionally opens them in browser

## Maintaining Mappings

### For the InternalAI Project

The current mappings cover:

- **Team Management**: View, edit, delete team members
- **Course Management**: List, create, edit, approve, archive courses
- **Course Enrollment**: Enroll team members in courses
- **Authentication**: Login, password reset, email verification

### Adding Screenshots

1. Capture screenshots of GUI features (full size and thumbnail)
2. Place them in this repository: `docs/screenshots/`
3. Update `GuiSupportMappings.json` with relative paths
4. Screenshots will be served from GitHub when using `screenshotBaseUrl`

**Screenshot Guidelines:**
- **Location**: `docs/screenshots/` in this repository
- **Full size**: Original resolution (e.g., 1920x1080)
- **Thumbnails**: 300x200px
- **Format**: PNG or JPEG
- **Naming**: Lowercase with hyphens (e.g., `team-list.png`, `team-list-thumb.png`)

**Example:**
```bash
# In this repository (API-first-LLM-orchestration)
cd docs/screenshots

# Capture screenshots from InternalAI frontend running at http://localhost:3000
# Save full-size screenshot as team-list.png

# Create thumbnail (using ImageMagick or online tool)
magick convert team-list.png -resize 300x200^ -gravity center -extent 300x200 team-list-thumb.png

# Commit and push
git add team-list.png team-list-thumb.png
git commit -m "Add team list screenshot"
git push
```

See `docs/screenshots/README.md` for detailed instructions and requirements.

### Adding New Mappings

When new GUI features are added to the frontend:

1. Identify the API operation ID from the Swagger document
2. Determine the GUI route where the feature is available
3. Capture screenshots if desired
4. Add a new mapping to `GuiSupportMappings.json`
5. Restart the MCP server (or reload configuration if supported)

### Example Workflow

1. Frontend team adds new "Projects" page at `/projects`
2. Backend exposes `GetProjects` operation
3. Capture screenshots and create thumbnails
4. Update `GuiSupportMappings.json`:

```json
{
  "operationId": "GetProjects",
  "guiRoute": "/projects",
  "guiFeature": "Projects List",
  "description": "View and manage all projects",
  "screenshotUrl": "projects-list.png",
  "thumbnailUrl": "projects-list-thumb.png"
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
Console.WriteLine($"Screenshot: {mapping.ScreenshotUrl}");

// Construct full GUI URL with parameters
var url = provider.GetGuiUrl("GetTeamMember", new Dictionary<string, string>
{
    ["memberId"] = "123"
});
// Returns: "http://localhost:3000/team/123"
```

## Benefits

1. **Discovery**: Clients can discover which operations have GUI alternatives
2. **Visualization**: Screenshots provide instant preview of GUI features
3. **User Guidance**: Help users choose between API and GUI based on their needs
4. **Quick Access**: One-click browser preview of GUI features
5. **Documentation**: Serves as living documentation of API-GUI relationships
6. **Integration**: Enables hybrid workflows combining API automation and GUI interaction

## Related Files

- `src/ApiFirst.LlmOrchestration.McpServer/GuiSupportMappings.json` - Configuration file
- `src/ApiFirst.LlmOrchestration/Models/GuiSupportMapping.cs` - Data models
- `src/ApiFirst.LlmOrchestration/Swagger/GuiSupportProvider.cs` - Service implementation
- `src/ApiFirst.LlmOrchestration.McpServer/McpServer.cs` - MCP integration
