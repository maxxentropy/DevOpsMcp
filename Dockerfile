# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy solution and project files
COPY DevOpsMcp.sln ./
COPY src/DevOpsMcp.Domain/DevOpsMcp.Domain.csproj src/DevOpsMcp.Domain/
COPY src/DevOpsMcp.Application/DevOpsMcp.Application.csproj src/DevOpsMcp.Application/
COPY src/DevOpsMcp.Infrastructure/DevOpsMcp.Infrastructure.csproj src/DevOpsMcp.Infrastructure/
COPY src/DevOpsMcp.Contracts/DevOpsMcp.Contracts.csproj src/DevOpsMcp.Contracts/
COPY src/DevOpsMcp.Server/DevOpsMcp.Server.csproj src/DevOpsMcp.Server/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY src/ src/

# Build and publish
WORKDIR /src/src/DevOpsMcp.Server
RUN dotnet publish -c Release -o /app/publish \
    --no-restore \
    -p:PublishAot=true \
    -p:PublishTrimmed=true \
    -p:TrimMode=partial

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-alpine AS runtime
WORKDIR /app

# Install required packages
RUN apk add --no-cache \
    ca-certificates \
    icu-libs \
    tzdata

# Create non-root user
RUN addgroup -g 1000 -S appuser && \
    adduser -u 1000 -S appuser -G appuser

# Copy published app
COPY --from=build --chown=appuser:appuser /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

# Switch to non-root user
USER appuser

# Expose ports
EXPOSE 8080

# Run the app
ENTRYPOINT ["./DevOpsMcp.Server"]