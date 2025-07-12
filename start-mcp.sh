#!/bin/bash

# MCP Server Management Script
# Start MCP servers independently with configurable database connections

set -e

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

print_info() {
    echo -e "${BLUE}ℹ️  $1${NC}"
}

# Default connection strings
MYSQL_CONNECTION="Server=localhost;Port=3306;Database=rent_wizard;Uid=mcpuser;Pwd=mcppass123;"
MSSQL_CONNECTION="Server=localhost,1433;Database=app_db;User Id=sa;Password=Temp@123;TrustServerCertificate=true;"

build_images() {
    print_warning "Building MCP server images..."
    docker compose build mcp-mysql mcp-mssql
    print_success "Images built successfully"
}

start_mcp_http() {
    local DB_TYPE="$1"
    local PORT="$2"
    local CONNECTION="$3"
    local CONTAINER_NAME="mcp-${DB_TYPE}-http"
    
    # Stop existing container if running
    docker rm -f "$CONTAINER_NAME" 2>/dev/null || true
    
    print_warning "Starting MCP server: $DB_TYPE + HTTP on port $PORT"
    
    docker run -d --name "$CONTAINER_NAME" \
        --network host \
        -e McpServer__Database__Provider="$DB_TYPE" \
        -e McpServer__Database__ConnectionString="$CONNECTION" \
        -e McpServer__Transport__Stdio__Enabled=false \
        -e McpServer__Transport__Http__Enabled=true \
        -e McpServer__Transport__Http__Port="$PORT" \
        -e ASPNETCORE_URLS="http://+:$PORT" \
        -p "$PORT:$PORT" \
        "mcp-dbserver-mcp-${DB_TYPE}:latest"
    
    # Wait for server to start
    sleep 5
    
    if curl -sf "http://localhost:$PORT/health" >/dev/null 2>&1; then
        print_success "MCP server started successfully!"
        echo ""
        echo "📡 Server Details:"
        echo "   Database: $DB_TYPE"
        echo "   Transport: HTTP"
        echo "   URL: http://localhost:$PORT"
        echo "   Health: http://localhost:$PORT/health"
        echo "   Swagger: http://localhost:$PORT/swagger"
        echo ""
        echo "🧪 Test Commands:"
        echo "   curl http://localhost:$PORT/health"
        echo "   curl -X POST http://localhost:$PORT/mcp/tools/list -H 'Content-Type: application/json' -d '{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\",\"params\":{}}'"
    else
        print_error "MCP server failed to start properly"
        docker logs "$CONTAINER_NAME" --tail 20
        exit 1
    fi
}

start_mcp_stdio() {
    local DB_TYPE="$1"
    local CONNECTION="$2"
    
    print_warning "Generating STDIO configuration for: $DB_TYPE"
    
    local VSCODE_CONFIG="{\n  \"mcpServers\": {\n    \"${DB_TYPE}-stdio\": {\n      \"command\": \"docker\",\n      \"args\": [\n        \"run\", \"--rm\", \"-i\", \"--network\", \"host\",\n        \"-e\", \"McpServer__Database__Provider=${DB_TYPE}\",\n        \"-e\", \"McpServer__Database__ConnectionString=${CONNECTION}\",\n        \"-e\", \"McpServer__Transport__Stdio__Enabled=true\",\n        \"-e\", \"McpServer__Transport__Http__Enabled=false\",\n        \"mcp-dbserver-mcp-${DB_TYPE}:latest\",\n        \"dotnet\", \"MsDbServer.dll\"\n      ]\n    }\n  }\n}"
    
    print_success "STDIO mode configuration generated!"
    echo ""
    echo "📋 VS Code Configuration (.vscode/mcp.json):"
    echo "=============================================="
    echo -e "$VSCODE_CONFIG"
    echo ""
    echo "🧪 Manual Test Command:"
    echo "======================="
    echo "echo '{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\",\"params\":{}}' | \\"
    echo "docker run --rm -i --network host \\"
    echo "  -e McpServer__Database__Provider=$DB_TYPE \\"
    echo "  -e McpServer__Database__ConnectionString=\"$CONNECTION\" \\"
    echo "  -e McpServer__Transport__Stdio__Enabled=true \\"
    echo "  -e McpServer__Transport__Http__Enabled=false \\"
    echo "  mcp-dbserver-mcp-${DB_TYPE}:latest \\"
    echo "  dotnet MsDbServer.dll"
    echo ""
    echo "📝 Next Steps:"
    echo "1. Copy the VS Code configuration above to .vscode/mcp.json"
    echo "2. Reload VS Code window"
    echo "3. Check MCP status in VS Code status bar"
}

stop_mcp() {
    print_warning "Stopping all MCP server containers..."
    docker rm -f mcp-mysql-http mcp-mssql-http 2>/dev/null || true
    print_success "All MCP server containers stopped"
}

status_mcp() {
    echo "MCP Server Status:"
    echo "=================="
    
    # Check MySQL HTTP
    if docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" | grep -q "mcp-mysql-http"; then
        print_success "MySQL HTTP: Running"
        docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" | grep "mcp-mysql-http"
    else
        echo "MySQL HTTP: Stopped"
    fi
    
    # Check MSSQL HTTP  
    if docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" | grep -q "mcp-mssql-http"; then
        print_success "MSSQL HTTP: Running"
        docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" | grep "mcp-mssql-http"
    else
        echo "MSSQL HTTP: Stopped"
    fi
}

show_usage() {
    echo "MCP Server Management"
    echo "===================="
    echo ""
    echo "Usage: $0 <database> <transport> [options]"
    echo ""
    echo "Databases:"
    echo "  mysql   - Use MySQL database"
    echo "  mssql   - Use MSSQL database"
    echo ""
    echo "Transport:"
    echo "  http    - Start HTTP server (for external access)"
    echo "  stdio   - Generate STDIO config (for VS Code)"
    echo ""
    echo "Options for HTTP mode:"
    echo "  --port <port>       - Custom port (default: 8080 for MySQL, 8081 for MSSQL)"
    echo "  --connection <str>  - Custom connection string"
    echo ""
    echo "Management Commands:"
    echo "  build   - Build MCP server Docker images"
    echo "  stop    - Stop all running MCP servers"
    echo "  status  - Show status of running MCP servers"
    echo ""
    echo "Examples:"
    echo "  $0 mysql http                    # Start MySQL HTTP server on port 8080"
    echo "  $0 mysql http --port 9000        # Start MySQL HTTP server on port 9000"
    echo "  $0 mssql stdio                   # Generate MSSQL STDIO config for VS Code"
    echo "  $0 mysql http --connection \"Server=remote.db;...\"  # Use remote MySQL"
    echo "  $0 build                         # Build Docker images"
    echo "  $0 stop                          # Stop all servers"
    echo ""
    echo "Connection String Examples:"
    echo "  MySQL:  \"Server=host;Port=3306;Database=db;Uid=user;Pwd=pass;\""
    echo "  MSSQL:  \"Server=host,1433;Database=db;User Id=user;Password=pass;TrustServerCertificate=true;\""
}

# Parse arguments
DB_TYPE=""
TRANSPORT=""
PORT=""
CONNECTION=""

while [[ $# -gt 0 ]]; do
    case $1 in
        mysql|mssql)
            DB_TYPE="$1"
            shift
            ;;
        http|stdio)
            TRANSPORT="$1"
            shift
            ;;
        --port)
            PORT="$2"
            shift 2
            ;;
        --connection)
            CONNECTION="$2"
            shift 2
            ;;
        build)
            build_images
            exit 0
            ;;
        stop)
            stop_mcp
            exit 0
            ;;
        status)
            status_mcp
            exit 0
            ;;
        -h|--help)
            show_usage
            exit 0
            ;;
        *)
            print_error "Unknown argument: $1"
            show_usage
            exit 1
            ;;
    esac
done

# Validate required arguments
if [[ -z "$DB_TYPE" || -z "$TRANSPORT" ]]; then
    print_error "Database type and transport mode are required"
    show_usage
    exit 1
fi

# Set default connection strings and ports
if [[ -z "$CONNECTION" ]]; then
    case "$DB_TYPE" in
        mysql)
            CONNECTION="$MYSQL_CONNECTION"
            ;;
        mssql)
            CONNECTION="$MSSQL_CONNECTION"
            ;;
    esac
fi

if [[ -z "$PORT" ]]; then
    case "$DB_TYPE" in
        mysql)
            PORT="8080"
            ;;
        mssql)
            PORT="8081"
            ;;
    esac
fi

# Execute based on transport mode
case "$TRANSPORT" in
    http)
        start_mcp_http "$DB_TYPE" "$PORT" "$CONNECTION"
        ;;
    stdio)
        start_mcp_stdio "$DB_TYPE" "$CONNECTION"
        ;;
    *)
        print_error "Invalid transport mode: $TRANSPORT"
        show_usage
        exit 1
        ;;
esac
