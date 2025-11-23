# Multi-stage Dockerfile for Blazor Performance Monitor Example
# Supports both x64 (amd64) and ARM64 architectures
# Build context should be the repository root

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src

# Install Python (required by Emscripten for WASM compilation)
RUN apt-get update && apt-get install -y python3 python3-pip && \
    ln -s /usr/bin/python3 /usr/bin/python && \
    rm -rf /var/lib/apt/lists/*

# Install wasm-tools workload for Blazor WebAssembly
RUN dotnet workload install wasm-tools

# Copy solution and project files
COPY ["BlazorPerfMon.sln", "./"]
COPY ["examples/ModelingEvolution.BlazorPerfMon.Example/ModelingEvolution.BlazorPerfMon.Example.sln", "examples/ModelingEvolution.BlazorPerfMon.Example/"]
COPY ["examples/ModelingEvolution.BlazorPerfMon.Example/ModelingEvolution.BlazorPerfMon.Example/ModelingEvolution.BlazorPerfMon.Example.csproj", "examples/ModelingEvolution.BlazorPerfMon.Example/ModelingEvolution.BlazorPerfMon.Example/"]
COPY ["examples/ModelingEvolution.BlazorPerfMon.Example/ModelingEvolution.BlazorPerfMon.Example.Client/ModelingEvolution.BlazorPerfMon.Example.Client.csproj", "examples/ModelingEvolution.BlazorPerfMon.Example/ModelingEvolution.BlazorPerfMon.Example.Client/"]
COPY ["src/ModelingEvolution.BlazorPerfMon.Server/ModelingEvolution.BlazorPerfMon.Server.csproj", "src/ModelingEvolution.BlazorPerfMon.Server/"]
COPY ["src/ModelingEvolution.BlazorPerfMon.Client/ModelingEvolution.BlazorPerfMon.Client.csproj", "src/ModelingEvolution.BlazorPerfMon.Client/"]
COPY ["src/ModelingEvolution.BlazorPerfMon.Shared/ModelingEvolution.BlazorPerfMon.Shared.csproj", "src/ModelingEvolution.BlazorPerfMon.Shared/"]

# Restore dependencies
RUN dotnet restore "examples/ModelingEvolution.BlazorPerfMon.Example/ModelingEvolution.BlazorPerfMon.Example/ModelingEvolution.BlazorPerfMon.Example.csproj"

# Copy source code
COPY ["src/", "src/"]
COPY ["examples/", "examples/"]

# Build and publish
WORKDIR "/src/examples/ModelingEvolution.BlazorPerfMon.Example/ModelingEvolution.BlazorPerfMon.Example"
RUN dotnet build "ModelingEvolution.BlazorPerfMon.Example.csproj" -c Release -o /app/build

RUN dotnet publish "ModelingEvolution.BlazorPerfMon.Example.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ARG TARGETARCH
WORKDIR /app

# Install dependencies for system monitoring
# Install nvidia-smi for GPU monitoring (optional - will gracefully degrade if not available)
# Install docker CLI for Docker container monitoring (optional)
RUN apt-get update && apt-get install -y \
    procps \
    sysstat \
    && rm -rf /var/lib/apt/lists/*

# Optional: Install NVIDIA drivers and tools for GPU monitoring
# Uncomment the following lines if running on a system with NVIDIA GPU
# RUN apt-get update && apt-get install -y --no-install-recommends \
#     nvidia-utils-535 \
#     && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Run as root for access to tegrastats and system monitoring
# Note: This is required for Tegra GPU monitoring and Docker socket access

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "ModelingEvolution.BlazorPerfMon.Example.dll"]
