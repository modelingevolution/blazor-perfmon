#!/bin/bash

# Deploy script for Blazor Performance Monitor
# Builds, stops old container, and runs new one with monitoring

set -e  # Exit on error

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "╔════════════════════════════════════════════════════════════════╗"
echo "║  Blazor Performance Monitor - Deploy Script                  ║"
echo "╚════════════════════════════════════════════════════════════════╝"
echo ""

# Step 1: Build Docker image
echo "[1/4] Building Docker image..."
./build.example.sh --arm64-only --load

# Step 2: Stop old container if running
echo ""
echo "[2/4] Stopping old container..."
if docker ps -q --filter "name=perfmon" | grep -q .; then
    docker stop perfmon
    echo "✓ Container stopped"
else
    echo "✓ No running container found"
fi

# Step 3: Remove old container if exists
echo ""
echo "[3/4] Removing old container..."
if docker ps -aq --filter "name=perfmon" | grep -q .; then
    docker rm perfmon
    echo "✓ Container removed"
else
    echo "✓ No container to remove"
fi

# Step 4: Run new container
echo ""
echo "[4/4] Starting new container..."
APPSETTINGS_PATH="${APPSETTINGS_PATH:-}"
if [ -z "$APPSETTINGS_PATH" ]; then
    # Try to detect Tegra platform
    if [ -f "/usr/bin/tegrastats" ]; then
        APPSETTINGS_PATH="$SCRIPT_DIR/appsettings.tegra.json"
        echo "Tegra platform detected, using: $APPSETTINGS_PATH"
    fi
fi

# Run container in background
APPSETTINGS_PATH="$APPSETTINGS_PATH" ./run.example.sh &
RUN_PID=$!

# Wait for container to start
echo ""
echo "Waiting for container to start..."
sleep 3

# Check if container is running
if docker ps --filter "name=perfmon" --format "{{.Names}}" | grep -q "perfmon"; then
    echo ""
    echo "✓ Container started successfully!"
    echo ""
    echo "╔════════════════════════════════════════════════════════════════╗"
    echo "║  Container Status                                             ║"
    echo "╚════════════════════════════════════════════════════════════════╝"
    docker ps --filter "name=perfmon" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
    echo ""

    # Determine URL based on networking mode
    if docker inspect perfmon --format '{{.HostConfig.NetworkMode}}' | grep -q "host"; then
        # Host networking mode
        PORT=$(docker exec perfmon printenv ASPNETCORE_URLS 2>/dev/null | grep -oP '(?<=:)\d+' || echo "5000")
        URL="http://localhost:${PORT}"
    else
        # Bridge mode with port mapping
        PORT=$(docker port perfmon 2>/dev/null | grep -oP '0.0.0.0:\K\d+' | head -1)
        URL="http://localhost:${PORT:-5000}"
    fi

    echo "Application URL: $URL"
    echo ""

    # Monitor logs
    echo "╔════════════════════════════════════════════════════════════════╗"
    echo "║  Container Logs (Ctrl+C to stop monitoring)                  ║"
    echo "╚════════════════════════════════════════════════════════════════╝"
    echo ""

    # Follow logs
    docker logs -f perfmon
else
    echo ""
    echo "✗ Container failed to start!"
    echo ""
    echo "Checking logs..."
    docker logs perfmon 2>&1 || echo "No logs available"
    exit 1
fi
