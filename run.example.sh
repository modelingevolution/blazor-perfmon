#!/bin/bash

# Run Blazor Performance Monitor Docker Container
# Automatically removes container on exit (--rm)
# Includes NVIDIA GPU support for nvidia-smi
# Automatically detects Jetson/Tegra platform and uses appropriate config

# Detect Jetson/Tegra platform and auto-configure
TEGRASTATS_ARGS=""
if [ -f "/usr/bin/tegrastats" ]; then
  echo "Tegra platform detected, mounting tegrastats"
  TEGRASTATS_ARGS="-v /usr/bin/tegrastats:/usr/bin/tegrastats:ro"

  # Auto-select Tegra config if not explicitly set
  if [ -z "$APPSETTINGS_PATH" ]; then
    # Get script directory to find config file
    SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
    TEGRA_CONFIG="$SCRIPT_DIR/examples/ModelingEvolution.BlazorPerfMon.Example/appsettings.tegra.json"
    if [ -f "$TEGRA_CONFIG" ]; then
      APPSETTINGS_PATH="$TEGRA_CONFIG"
      echo "Auto-selected Tegra configuration"
    fi
  fi
fi

# Determine config file to use
CONFIG_VOLUME_ARGS=""
if [ -n "$APPSETTINGS_PATH" ] && [ -f "$APPSETTINGS_PATH" ]; then
  echo "Using custom appsettings from: $APPSETTINGS_PATH"
  CONFIG_VOLUME_ARGS="-v $(realpath "$APPSETTINGS_PATH"):/app/appsettings.json:ro"
fi

docker run --rm \
  --name perfmon \
  --privileged \
  --network host \
  -e ASPNETCORE_URLS=http://0.0.0.0:5000 \
  -v /var/run/docker.sock:/var/run/docker.sock:ro \
  -v /proc:/host/proc:ro \
  --cap-add SYS_ADMIN \
  --gpus all \
  $CONFIG_VOLUME_ARGS \
  $TEGRASTATS_ARGS \
  modelingevolution/blazor-perfmon-example:latest
