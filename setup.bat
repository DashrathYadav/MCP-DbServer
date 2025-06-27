@echo off
REM Setup script for MsDbServer on Windows

echo 🚀 Setting up MsDbServer...
echo ==========================

REM Check if .NET 8 is installed
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ .NET 8 SDK is not installed
    echo Please install .NET 8 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

REM Display .NET version
echo ✅ .NET SDK installed
dotnet --version

REM Build the project
echo 🔨 Building MsDbServer...
cd MsDbServer
dotnet restore
dotnet build
if %errorlevel% neq 0 (
    echo ❌ Build failed
    pause
    exit /b 1
)

echo ✅ Build successful!

REM Create MCP configuration if it doesn't exist
if not exist "..\.vscode\mcp.json" (
    echo 📝 Creating MCP configuration...
    if not exist "..\.vscode" mkdir "..\.vscode"
    copy "..\mcp.json.example" "..\.vscode\mcp.json"
    echo ✅ Created .vscode\mcp.json
    echo ⚠️  Please edit .vscode\mcp.json with your database connection details
) else (
    echo ✅ MCP configuration already exists
)

echo.
echo 🎉 Setup complete!
echo.
echo Next steps:
echo 1. Edit MsDbServer\appsettings.json with your database connection
echo 2. Edit .vscode\mcp.json with your database connection
echo 3. Open VS Code: code .
echo 4. Test in GitHub Copilot Chat: 'List all tables in the database'
echo.
echo For detailed instructions, see README.md
pause
