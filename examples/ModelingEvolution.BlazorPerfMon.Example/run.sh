#!/bin/bash

# Run Blazor Performance Monitor Docker Container
# Automatically removes container on exit (--rm)
# Includes NVIDIA GPU support for nvidia-smi

docker run --rm \
  --name perfmon \
  -p 5000:8080 \
  -v /var/run/docker.sock:/var/run/docker.sock:ro \
  -v /proc:/host/proc:ro \
  --cap-add SYS_ADMIN \
  --gpus all \
  modelingevolution/blazor-perfmon-example:latest
