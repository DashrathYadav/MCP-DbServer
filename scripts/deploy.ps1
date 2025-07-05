# PowerShell Deployment Script for MCP Database Server
# This script provides easy deployment options for different scenarios

param(
    [Parameter(Position=0)]
    [ValidateSet("mysql", "postgresql", "dev", "full", "stop", "clean", "logs", "status", "build", "help")]
    [string]$Command = "help",
    
    [Parameter(Position=1)]
    [string]$Service = "",
    
    [switch]$Detach = $true,
    [switch]$Follow = $false,
    [switch]$Rebuild = $false
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir

Write-Host "🐳 MCP Database Server - Docker Deployment Script" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan

function Show-Usage {
    Write-Host @"
Usage: .\deploy.ps1 [COMMAND] [OPTIONS]

Commands:
  mysql       - Deploy MySQL MCP Server instance
  postgresql  - Deploy PostgreSQL MCP Server instance (future)
  dev         - Deploy development instance with STDIO
  full        - Deploy full stack with load balancer
  stop        - Stop all services
  clean       - Stop and remove all containers and volumes
  logs        - Show logs for all services
  status      - Show status of all services
  build       - Build the MCP server image

Options:
  -Detach     - Run in detached mode (default: true)
  -Follow     - Follow logs after deployment
  -Rebuild    - Force rebuild of containers

Examples:
  .\deploy.ps1 mysql                     # Deploy MySQL instance
  .\deploy.ps1 full -Rebuild             # Deploy full stack with rebuild
  .\deploy.ps1 dev -Follow               # Deploy dev instance and follow logs
  .\deploy.ps1 logs mysql                # Show logs for MySQL instance
"@
}

function Build-Image {
    Write-Host "🔨 Building MCP Database Server image..." -ForegroundColor Yellow
    Set-Location $ProjectRoot
    docker build -t mcp-database-server:latest .
    Write-Host "✅ Image built successfully" -ForegroundColor Green
}

function Deploy-MySQL {
    Write-Host "🚀 Deploying MySQL MCP Server instance..." -ForegroundColor Yellow
    Set-Location $ProjectRoot
    
    if ($Rebuild) {
        docker-compose build --no-cache mcp-mysql
    }
    
    $detachFlag = if ($Detach -and -not $Follow) { "-d" } else { "" }
    Invoke-Expression "docker-compose up $detachFlag mcp-mysql mysql-db"
    
    if ($Follow) {
        docker-compose logs -f mcp-mysql
    }
    
    Write-Host "✅ MySQL MCP Server deployed" -ForegroundColor Green
    Write-Host "📍 Access: http://localhost:8080" -ForegroundColor Cyan
    Write-Host "🔍 Health: http://localhost:8080/health" -ForegroundColor Cyan
    Write-Host "📖 Swagger: http://localhost:8080/swagger" -ForegroundColor Cyan
}

function Deploy-PostgreSQL {
    Write-Host "🚀 Deploying PostgreSQL MCP Server instance..." -ForegroundColor Yellow
    Set-Location $ProjectRoot
    
    if ($Rebuild) {
        docker-compose build --no-cache mcp-postgresql
    }
    
    $detachFlag = if ($Detach -and -not $Follow) { "-d" } else { "" }
    Invoke-Expression "docker-compose --profile postgresql up $detachFlag mcp-postgresql postgres-db"
    
    if ($Follow) {
        docker-compose logs -f mcp-postgresql
    }
    
    Write-Host "✅ PostgreSQL MCP Server deployed" -ForegroundColor Green
    Write-Host "📍 Access: http://localhost:8081" -ForegroundColor Cyan
    Write-Host "🔍 Health: http://localhost:8081/health" -ForegroundColor Cyan
    Write-Host "📖 Swagger: http://localhost:8081/swagger" -ForegroundColor Cyan
}

function Deploy-Dev {
    Write-Host "🛠️ Deploying Development MCP Server instance..." -ForegroundColor Yellow
    Set-Location $ProjectRoot
    
    if ($Rebuild) {
        docker-compose build --no-cache mcp-dev
    }
    
    $detachFlag = if ($Detach -and -not $Follow) { "-d" } else { "" }
    Invoke-Expression "docker-compose --profile development up $detachFlag mcp-dev mysql-db"
    
    if ($Follow) {
        docker-compose logs -f mcp-dev
    }
    
    Write-Host "✅ Development MCP Server deployed" -ForegroundColor Green
    Write-Host "🔌 STDIO transport enabled for development" -ForegroundColor Cyan
}

function Deploy-Full {
    Write-Host "🌟 Deploying Full Stack with Load Balancer..." -ForegroundColor Yellow
    Set-Location $ProjectRoot
    
    if ($Rebuild) {
        docker-compose build --no-cache
    }
    
    $detachFlag = if ($Detach -and -not $Follow) { "-d" } else { "" }
    Invoke-Expression "docker-compose --profile loadbalancer up $detachFlag"
    
    if ($Follow) {
        docker-compose logs -f
    }
    
    Write-Host "✅ Full Stack deployed" -ForegroundColor Green
    Write-Host "🌐 Load Balancer: http://localhost" -ForegroundColor Cyan
    Write-Host "🔍 MySQL Instance: http://mysql.mcp.local (add to hosts file)" -ForegroundColor Cyan
    Write-Host "🔍 PostgreSQL Instance: http://postgresql.mcp.local (add to hosts file)" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Add these entries to your C:\Windows\System32\drivers\etc\hosts file:" -ForegroundColor Yellow
    Write-Host "127.0.0.1 mysql.mcp.local" -ForegroundColor White
    Write-Host "127.0.0.1 postgresql.mcp.local" -ForegroundColor White
}

function Stop-Services {
    Write-Host "🛑 Stopping all MCP services..." -ForegroundColor Yellow
    Set-Location $ProjectRoot
    docker-compose down
    Write-Host "✅ All services stopped" -ForegroundColor Green
}

function Clean-Up {
    Write-Host "🧹 Cleaning up all containers and volumes..." -ForegroundColor Yellow
    Set-Location $ProjectRoot
    docker-compose down -v --remove-orphans
    docker system prune -f
    Write-Host "✅ Cleanup completed" -ForegroundColor Green
}

function Show-Logs {
    Set-Location $ProjectRoot
    if ($Service) {
        Write-Host "📋 Showing logs for $Service..." -ForegroundColor Yellow
        docker-compose logs -f $Service
    } else {
        Write-Host "📋 Showing logs for all services..." -ForegroundColor Yellow
        docker-compose logs -f
    }
}

function Show-Status {
    Write-Host "📊 MCP Services Status:" -ForegroundColor Cyan
    Write-Host "======================" -ForegroundColor Cyan
    Set-Location $ProjectRoot
    docker-compose ps
    Write-Host ""
    Write-Host "💾 Volume Usage:" -ForegroundColor Cyan
    $volumes = docker volume ls | Select-String "mcp"
    if ($volumes) { $volumes } else { Write-Host "No MCP volumes found" }
    Write-Host ""
    Write-Host "🌐 Network Status:" -ForegroundColor Cyan
    $networks = docker network ls | Select-String "mcp"
    if ($networks) { $networks } else { Write-Host "No MCP networks found" }
}

# Main command handling
switch ($Command.ToLower()) {
    "mysql" { Deploy-MySQL }
    "postgresql" { Deploy-PostgreSQL }
    "dev" { Deploy-Dev }
    "full" { Deploy-Full }
    "stop" { Stop-Services }
    "clean" { Clean-Up }
    "logs" { Show-Logs }
    "status" { Show-Status }
    "build" { Build-Image }
    "help" { Show-Usage }
    default {
        Write-Host "❌ Unknown command: $Command" -ForegroundColor Red
        Write-Host ""
        Show-Usage
        exit 1
    }
}
