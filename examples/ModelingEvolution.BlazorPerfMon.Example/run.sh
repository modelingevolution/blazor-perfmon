#!/bin/bash

# Run Blazor Performance Monitor Docker Container
# Automatically removes container on exit (--rm)
# Includes NVIDIA GPU support for nvidia-smi
# Uses custom Tegra configuration when APPSETTINGS_PATH is set

# Determine config file to use
APPSETTINGS_PATH="${APPSETTINGS_PATH:-}"
CONFIG_VOLUME_ARGS=""

if [ -n "$APPSETTINGS_PATH" ] && [ -f "$APPSETTINGS_PATH" ]; then
  echo "Using custom appsettings from: $APPSETTINGS_PATH"
  CONFIG_VOLUME_ARGS="-v $(realpath "$APPSETTINGS_PATH"):/app/appsettings.json:ro"
fi

docker run --rm \
  --name perfmon \
  --privileged \
  -p 5000:8080 \
  -v /var/run/docker.sock:/var/run/docker.sock:ro \
  -v /proc:/host/proc:ro \
  --cap-add SYS_ADMIN \
  --gpus all \
  $CONFIG_VOLUME_ARGS \
  modelingevolution/blazor-perfmon-example:latest
