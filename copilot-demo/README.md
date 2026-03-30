# Copilot Demo Package Starter

This folder is a starter package for using the local demo host with Microsoft 365 Copilot / the Copilot app.

## What you do first

1. Run the demo host locally.
2. Expose it with a tunnel so Copilot can reach it.
3. Point the plugin manifest at the tunnel URL's `/openapi.json` endpoint.
4. Use the declarative agent manifest to reference the API plugin.

## Local host

Run:

```powershell
dotnet run --project ..\src\ApiFirst.LlmOrchestration.DemoHost -- --urls http://localhost:5055
```

Then create a tunnel that forwards to port `5055`.

## Files

- `openapi.demo-host.json` - sample OpenAPI document for the demo host
- `declarative-agent.json` - starter declarative agent manifest
- `api-plugin.json` - starter API plugin manifest

## Notes

- Copilot can't call `localhost` directly; the host must be internet-reachable.
- The demo host serves `/openapi.json` dynamically, so a tunnel URL can point there without manual edits to the demo host.
- The manifest files here are starter files. Validate them with the Microsoft 365 Agents Toolkit before sideloading.
