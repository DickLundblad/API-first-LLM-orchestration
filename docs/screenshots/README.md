# InternalAI GUI Screenshots

This directory contains screenshots and thumbnails of the InternalAI frontend application to provide visual documentation of GUI features that correspond to API operations.

## Directory Structure

```
screenshots/
├── README.md                    # This file
├── login.png                    # Login page (full size)
├── login-thumb.png              # Login page (thumbnail)
├── team-list.png                # Team members list (full size)
├── team-list-thumb.png          # Team members list (thumbnail)
├── team-detail.png              # Team member detail page (full size)
├── team-detail-thumb.png        # Team member detail page (thumbnail)
├── courses-list.png             # Courses page (full size)
├── courses-list-thumb.png       # Courses page (thumbnail)
├── forgot-password.png          # Forgot password page (full size)
├── forgot-password-thumb.png    # Forgot password page (thumbnail)
├── reset-password.png           # Reset password page (full size)
├── reset-password-thumb.png     # Reset password page (thumbnail)
├── verify-email.png             # Email verification page (full size)
└── verify-email-thumb.png       # Email verification page (thumbnail)
```

## Screenshot Guidelines

### Size Requirements

- **Full Screenshots**: Original size, typically 1920x1080 or browser window size
- **Thumbnails**: 300x200px (resize and crop to maintain aspect ratio)

### Format

- **File Format**: PNG (preferred) or JPEG
- **Color Depth**: 24-bit color
- **Compression**: Optimize for web (balance quality and file size)

### Naming Convention

- Use lowercase with hyphens: `feature-name.png`
- Thumbnails: Add `-thumb` suffix: `feature-name-thumb.png`
- Match the names in `GuiSupportMappings.json`

### Capture Guidelines

1. **Browser**: Use latest Chrome or Edge
2. **Window Size**: 1920x1080 or consistent size across all screenshots
3. **Zoom Level**: 100% (default)
4. **Clean State**: 
   - Remove browser extensions UI
   - Use sample/test data
   - Hide personal information
5. **Focus**: Capture the main feature being documented
6. **Annotations**: Optional arrows/highlights for complex features

## How to Capture Screenshots

### Using Browser DevTools

1. Open InternalAI frontend at `http://localhost:3000`
2. Navigate to the page/feature you want to capture
3. Press `F12` to open DevTools
4. Press `Ctrl+Shift+P` (Windows) or `Cmd+Shift+P` (Mac)
5. Type "screenshot" and select "Capture full size screenshot"
6. Save with appropriate name

### Using Snipping Tool (Windows)

1. Open the page in browser
2. Press `Win+Shift+S`
3. Select area to capture
4. Save from clipboard to file

### Creating Thumbnails

#### Using PowerShell (Windows)

```powershell
# Install ImageMagick first: winget install ImageMagick.ImageMagick

# Create thumbnail (300x200)
magick convert login.png -resize 300x200^ -gravity center -extent 300x200 login-thumb.png
```

#### Using Online Tools

- [TinyPNG](https://tinypng.com/) - Compress images
- [Squoosh](https://squoosh.app/) - Resize and optimize
- [ILoveIMG](https://www.iloveimg.com/resize-image) - Batch resize

## Required Screenshots

Based on `GuiSupportMappings.json`, the following screenshots are needed:

- [ ] `login.png` + `login-thumb.png`
- [ ] `team-list.png` + `team-list-thumb.png`
- [ ] `team-detail.png` + `team-detail-thumb.png`
- [ ] `courses-list.png` + `courses-list-thumb.png`
- [ ] `forgot-password.png` + `forgot-password-thumb.png`
- [ ] `reset-password.png` + `reset-password-thumb.png`
- [ ] `verify-email.png` + `verify-email-thumb.png`

## Publishing Screenshots

Once you've added screenshots:

```bash
# From the repository root
git add docs/screenshots/*.png
git commit -m "Add GUI feature screenshots for MCP server"
git push origin main
```

Screenshots will be automatically available via GitHub at:
```
https://raw.githubusercontent.com/DickLundblad/API-first-LLM-orchestration/main/docs/screenshots/{filename}
```

## Usage in MCP Server

These screenshots are referenced in `src/ApiFirst.LlmOrchestration.McpServer/GuiSupportMappings.json`:

```json
{
  "screenshotBaseUrl": "https://raw.githubusercontent.com/DickLundblad/API-first-LLM-orchestration/main/docs/screenshots",
  "mappings": [
    {
      "operationId": "Login",
      "screenshotUrl": "login.png",
      "thumbnailUrl": "login-thumb.png"
    }
  ]
}
```

When MCP clients query operations, they receive full screenshot URLs:
```
https://raw.githubusercontent.com/DickLundblad/API-first-LLM-orchestration/main/docs/screenshots/login.png
```

## Tips

- **Consistency**: Use same browser, window size, and theme across all screenshots
- **Sample Data**: Use realistic but fake data in screenshots
- **Privacy**: Never include real user names, emails, or sensitive information
- **Updates**: When GUI changes significantly, update screenshots
- **Optimization**: Keep file sizes reasonable (< 500KB for full, < 50KB for thumbs)
- **Testing**: After pushing, verify URLs work in browser

## Example Workflow

```bash
# 1. Start InternalAI frontend
cd C:\git\InternalAI\frontend
npm start

# 2. Capture screenshots (use browser or tools above)

# 3. Create thumbnails
cd C:\git\API-first-LLM-orchestration\docs\screenshots
magick convert login.png -resize 300x200^ -gravity center -extent 300x200 login-thumb.png

# 4. Commit and push
git add *.png
git commit -m "Add login page screenshot"
git push
```

## Troubleshooting

**Screenshots not loading in MCP client?**
- Verify GitHub URL works in browser
- Check screenshot file names match exactly in `GuiSupportMappings.json`
- Ensure files are committed and pushed to `main` branch
- GitHub may cache - try `?timestamp` query parameter

**Thumbnails look distorted?**
- Use `-resize 300x200^` flag to maintain aspect ratio
- Use `-gravity center -extent 300x200` to crop centered

## Related Files

- `../../src/ApiFirst.LlmOrchestration.McpServer/GuiSupportMappings.json` - Configuration
- `../GUI-Support.md` - Feature documentation
