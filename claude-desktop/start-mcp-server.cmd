@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "REPO_ROOT=%SCRIPT_DIR%..\"

rem Default swagger URL can be overridden by extra args from Claude Desktop.
dotnet run --project "%REPO_ROOT%src\ApiFirst.LlmOrchestration.McpServer" -- --swagger-url http://localhost:5000/api/swagger.json %*

endlocal
