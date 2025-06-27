#!/bin/bash
# Setup script for MsDbServer

echo "🚀 Setting up MsDbServer..."
echo "=========================="

# Check if .NET 8 is installed
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET 8 SDK is not installed"
    echo "Please install .NET 8 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
fi

# Check .NET version
DOTNET_VERSION=$(dotnet --version)
echo "✅ .NET SDK version: $DOTNET_VERSION"

# Build the project
echo "🔨 Building MsDbServer..."
cd MsDbServer
dotnet restore
if ! dotnet build; then
    echo "❌ Build failed"
    exit 1
fi

echo "✅ Build successful!"

# Create MCP configuration if it doesn't exist
if [ ! -f "../.vscode/mcp.json" ]; then
    echo "📝 Creating MCP configuration..."
    mkdir -p ../.vscode
    cp ../mcp.json.example ../.vscode/mcp.json
    echo "✅ Created .vscode/mcp.json"
    echo "⚠️  Please edit .vscode/mcp.json with your database connection details"
else
    echo "✅ MCP configuration already exists"
fi

echo ""
echo "🎉 Setup complete!"
echo ""
echo "Next steps:"
echo "1. Edit MsDbServer/appsettings.json with your database connection"
echo "2. Edit .vscode/mcp.json with your database connection"
echo "3. Open VS Code: code ."
echo "4. Test in GitHub Copilot Chat: 'List all tables in the database'"
echo ""
echo "For detailed instructions, see README.md"
