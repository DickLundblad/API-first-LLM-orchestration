@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "REPO_ROOT=%SCRIPT_DIR%..\"

rem If admin-specific vars are missing, fall back to OPENAI_API_KEY.
if not defined API_ORCH_ADMIN_API_KEY if defined OPENAI_API_KEY (
  if not defined API_ORCH_ADMIN_PROVIDER set "API_ORCH_ADMIN_PROVIDER=openai"
  if not defined API_ORCH_ADMIN_MODEL set "API_ORCH_ADMIN_MODEL=gpt-5.4-mini"
  set "API_ORCH_ADMIN_API_KEY=%OPENAI_API_KEY%"
)

rem Default swagger URL can be overridden by extra args from Claude Desktop.
dotnet run --project "%REPO_ROOT%src\ApiFirst.LlmOrchestration.McpServer" -- --swagger-url http://localhost:5000/api/swagger.json %*

endlocal
