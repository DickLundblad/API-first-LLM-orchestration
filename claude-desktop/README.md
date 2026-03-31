# Claude Desktop

Minimal local MCP setup for Windows.

Claude Desktop stores the config here on Windows:

`%APPDATA%\\Claude\\claude_desktop_config.json`

Use this setup to point Claude Desktop at the existing stdio MCP server:

1. Open `%APPDATA%\\Claude\\claude_desktop_config.json`.
2. Paste the contents of `claude_desktop_config.json.example`.
3. Update the `command` path so it matches your local repo path.
4. Restart Claude Desktop.
5. Ask Claude to search for `team`.

The launcher script `start-mcp-server.cmd` starts the server with:

- `--swagger-url http://localhost:5000/api/swagger.json`

You can add extra arguments in Claude config `args` if needed.
