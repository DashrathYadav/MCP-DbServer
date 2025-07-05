# Multi-stage build for MCP Database Server
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["MsDbServer/MsDbServer.csproj", "MsDbServer/"]

# Restore dependencies
RUN dotnet restore "MsDbServer/MsDbServer.csproj"

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/MsDbServer"
RUN dotnet build "MsDbServer.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "MsDbServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN groupadd -r mcpuser && useradd -r -g mcpuser mcpuser

# Copy published application
COPY --from=publish /app/publish .

# Create logs directory
RUN mkdir -p /app/logs && chown -R mcpuser:mcpuser /app

# Switch to non-root user
USER mcpuser

# Environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Expose ports
EXPOSE 8080

# Entry point
ENTRYPOINT ["dotnet", "MsDbServer.dll"]
