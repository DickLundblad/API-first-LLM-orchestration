# ChatGPT App Scaffold

Single-tool MVP for ChatGPT:

- ChatGPT connects to the MCP server directly
- use the `search_operations` tool to search swagger operations

Flow: ChatGPT -> HTTPS tunnel -> tiny HTTP wrapper -> MCP server -> REST API

