#!/bin/bash

# Docker Deployment Script for MCP Database Server
# This script provides easy deployment options for different scenarios

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

echo "🐳 MCP Database Server - Docker Deployment Script"
echo "=================================================="

# Function to show usage
show_usage() {
    echo "Usage: $0 [COMMAND] [OPTIONS]"
    echo ""
    echo "Commands:"
    echo "  mysql       - Deploy MySQL MCP Server instance"
    echo "  postgresql  - Deploy PostgreSQL MCP Server instance (future)"
    echo "  dev         - Deploy development instance with STDIO"
    echo "  full        - Deploy full stack with load balancer"
    echo "  stop        - Stop all services"
    echo "  clean       - Stop and remove all containers and volumes"
    echo "  logs        - Show logs for all services"
    echo "  status      - Show status of all services"
    echo "  build       - Build the MCP server image"
    echo ""
    echo "Options:"
    echo "  --detach    - Run in detached mode (default)"
    echo "  --follow    - Follow logs after deployment"
    echo "  --rebuild   - Force rebuild of containers"
    echo ""
    echo "Examples:"
    echo "  $0 mysql                    # Deploy MySQL instance"
    echo "  $0 full --rebuild           # Deploy full stack with rebuild"
    echo "  $0 dev --follow             # Deploy dev instance and follow logs"
    echo "  $0 logs mysql               # Show logs for MySQL instance"
}

# Function to build the image
build_image() {
    echo "🔨 Building MCP Database Server image..."
    cd "$PROJECT_ROOT"
    docker build -t mcp-database-server:latest .
    echo "✅ Image built successfully"
}

# Function to deploy MySQL instance
deploy_mysql() {
    echo "🚀 Deploying MySQL MCP Server instance..."
    cd "$PROJECT_ROOT"
    
    if [[ "$REBUILD" == "true" ]]; then
        docker-compose build --no-cache mcp-mysql
    fi
    
    docker-compose up $DETACH_FLAG mcp-mysql mysql-db
    
    if [[ "$FOLLOW_LOGS" == "true" ]]; then
        docker-compose logs -f mcp-mysql
    fi
    
    echo "✅ MySQL MCP Server deployed"
    echo "📍 Access: http://localhost:8080"
    echo "🔍 Health: http://localhost:8080/health"
    echo "📖 Swagger: http://localhost:8080/swagger"
}

# Function to deploy PostgreSQL instance
deploy_postgresql() {
    echo "🚀 Deploying PostgreSQL MCP Server instance..."
    cd "$PROJECT_ROOT"
    
    if [[ "$REBUILD" == "true" ]]; then
        docker-compose build --no-cache mcp-postgresql
    fi
    
    docker-compose --profile postgresql up $DETACH_FLAG mcp-postgresql postgres-db
    
    if [[ "$FOLLOW_LOGS" == "true" ]]; then
        docker-compose logs -f mcp-postgresql
    fi
    
    echo "✅ PostgreSQL MCP Server deployed"
    echo "📍 Access: http://localhost:8081"
    echo "🔍 Health: http://localhost:8081/health"
    echo "📖 Swagger: http://localhost:8081/swagger"
}

# Function to deploy development instance
deploy_dev() {
    echo "🛠️ Deploying Development MCP Server instance..."
    cd "$PROJECT_ROOT"
    
    if [[ "$REBUILD" == "true" ]]; then
        docker-compose build --no-cache mcp-dev
    fi
    
    docker-compose --profile development up $DETACH_FLAG mcp-dev mysql-db
    
    if [[ "$FOLLOW_LOGS" == "true" ]]; then
        docker-compose logs -f mcp-dev
    fi
    
    echo "✅ Development MCP Server deployed"
    echo "🔌 STDIO transport enabled for development"
}

# Function to deploy full stack
deploy_full() {
    echo "🌟 Deploying Full Stack with Load Balancer..."
    cd "$PROJECT_ROOT"
    
    if [[ "$REBUILD" == "true" ]]; then
        docker-compose build --no-cache
    fi
    
    docker-compose --profile loadbalancer up $DETACH_FLAG
    
    if [[ "$FOLLOW_LOGS" == "true" ]]; then
        docker-compose logs -f
    fi
    
    echo "✅ Full Stack deployed"
    echo "🌐 Load Balancer: http://localhost"
    echo "🔍 MySQL Instance: http://mysql.mcp.local (add to hosts file)"
    echo "🔍 PostgreSQL Instance: http://postgresql.mcp.local (add to hosts file)"
    echo ""
    echo "Add these entries to your /etc/hosts file:"
    echo "127.0.0.1 mysql.mcp.local"
    echo "127.0.0.1 postgresql.mcp.local"
}

# Function to stop services
stop_services() {
    echo "🛑 Stopping all MCP services..."
    cd "$PROJECT_ROOT"
    docker-compose --profile "*" down
    echo "✅ All services stopped"
}

# Function to clean up
clean_up() {
    echo "🧹 Cleaning up all containers and volumes..."
    cd "$PROJECT_ROOT"
    docker-compose --profile "*" down -v --remove-orphans
    docker system prune -f
    echo "✅ Cleanup completed"
}

# Function to show logs
show_logs() {
    cd "$PROJECT_ROOT"
    if [[ -n "$2" ]]; then
        echo "📋 Showing logs for $2..."
        docker-compose logs -f "$2"
    else
        echo "📋 Showing logs for all services..."
        docker-compose logs -f
    fi
}

# Function to show status
show_status() {
    echo "📊 MCP Services Status:"
    echo "======================"
    cd "$PROJECT_ROOT"
    docker-compose ps
    echo ""
    echo "💾 Volume Usage:"
    docker volume ls | grep mcp || echo "No MCP volumes found"
    echo ""
    echo "🌐 Network Status:"
    docker network ls | grep mcp || echo "No MCP networks found"
}

# Parse command line arguments
DETACH_FLAG="-d"
FOLLOW_LOGS="false"
REBUILD="false"

while [[ $# -gt 0 ]]; do
    case $1 in
        --detach)
            DETACH_FLAG="-d"
            shift
            ;;
        --follow)
            FOLLOW_LOGS="true"
            DETACH_FLAG=""
            shift
            ;;
        --rebuild)
            REBUILD="true"
            shift
            ;;
        *)
            break
            ;;
    esac
done

# Main command handling
case "$1" in
    "mysql")
        deploy_mysql
        ;;
    "postgresql")
        deploy_postgresql
        ;;
    "dev")
        deploy_dev
        ;;
    "full")
        deploy_full
        ;;
    "stop")
        stop_services
        ;;
    "clean")
        clean_up
        ;;
    "logs")
        show_logs "$@"
        ;;
    "status")
        show_status
        ;;
    "build")
        build_image
        ;;
    "help"|"--help"|"-h")
        show_usage
        ;;
    "")
        echo "❌ No command specified"
        echo ""
        show_usage
        exit 1
        ;;
    *)
        echo "❌ Unknown command: $1"
        echo ""
        show_usage
        exit 1
        ;;
esac
