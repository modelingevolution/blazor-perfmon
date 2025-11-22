# Docker Setup for Blazor Performance Monitor Example

This directory contains everything needed to run the Performance Monitor example in Docker, with support for both **x64 (amd64)** and **ARM64** architectures.

## Quick Start

### Using Docker Compose (Recommended)

```bash
cd examples/ModelingEvolution.BlazorPerfMon.Example
docker-compose up -d
```

Access the application at: http://localhost:5000

### Using Docker Run

```bash
# Build the image first
cd examples/ModelingEvolution.BlazorPerfMon.Example
./build-docker.sh --amd64-only --load

# Run the container (x64/amd64)
docker run -d \
  --name perfmon \
  -p 5000:8080 \
  -v /var/run/docker.sock:/var/run/docker.sock:ro \
  -v /proc:/host/proc:ro \
  --cap-add SYS_ADMIN \
  modelingevolution/blazor-perfmon-example:latest

# Run on NVIDIA Jetson (ARM64) with tegrastats GPU monitoring
docker run -d \
  --name perfmon \
  -p 5000:8080 \
  -v /var/run/docker.sock:/var/run/docker.sock:ro \
  -v /proc:/host/proc:ro \
  -v /usr/bin/tegrastats:/usr/bin/tegrastats:ro \
  --cap-add SYS_ADMIN \
  --cap-add NET_ADMIN \
  modelingevolution/blazor-perfmon-example:latest
```

## Architecture Support

The Dockerfile and build scripts support:
- **linux/amd64** (x64 Intel/AMD processors)
- **linux/arm64** (ARM64 processors like Raspberry Pi 4, Jetson, Apple Silicon via emulation)

## Files

- **Dockerfile** - Multi-stage build for the application
- **docker-compose.yml** - Docker Compose configuration with demo containers
- **build-docker.sh** - Build script for multi-platform images
- **.dockerignore** - Files to exclude from Docker build context (in repo root)

## Building Multi-Platform Images

### Build for All Platforms

```bash
./build-docker.sh
```

### Build for Specific Platform

```bash
# AMD64 only (x64)
./build-docker.sh --amd64-only --load

# ARM64 only
./build-docker.sh --arm64-only --load
```

### Build and Push to Registry

```bash
./build-docker.sh -t v1.0.0 --push
```

### Build Script Options

```
Options:
  -t, --tag TAG          Image tag (default: latest)
  -p, --push             Push images to registry
  -l, --load             Load image to local Docker (single platform only)
  --platform PLATFORMS   Comma-separated list of platforms
  --amd64-only          Build for AMD64 only
  --arm64-only          Build for ARM64 only
  -h, --help            Show help
```

## Configuration

### Environment Variables

Override configuration via environment variables in `docker-compose.yml`:

```yaml
environment:
  - MonitorSettings__NetworkInterface=eth0
  - MonitorSettings__DiskDevice=sda
  - MonitorSettings__CollectionIntervalMs=500
  - MonitorSettings__GpuCollectorType=NvSmi
```

### Volume Mounts

Required for monitoring:

```yaml
volumes:
  # Docker container monitoring
  - /var/run/docker.sock:/var/run/docker.sock:ro
  # System metrics (CPU, RAM, Network, Disk)
  - /proc:/host/proc:ro
```

### Capabilities

Required for accessing system metrics:

```yaml
cap_add:
  - SYS_ADMIN  # Required for /proc filesystem
  - NET_ADMIN  # Required for network statistics
```

## GPU Support

### NVIDIA Jetson (Tegra) Devices

For NVIDIA Jetson devices (Orin NX, AGX Orin, Xavier NX, etc.), the application uses the native `tegrastats` tool for GPU monitoring:

**Configuration**: Set `GpuCollectorType` to `"NvTegra"` in `appsettings.json`:

```json
{
  "MonitorSettings": {
    "GpuCollectorType": "NvTegra"
  }
}
```

**Docker Run**: Mount tegrastats from the host:

```bash
docker run -d \
  -p 5000:8080 \
  -v /var/run/docker.sock:/var/run/docker.sock:ro \
  -v /proc:/host/proc:ro \
  -v /usr/bin/tegrastats:/usr/bin/tegrastats:ro \
  --cap-add SYS_ADMIN \
  --cap-add NET_ADMIN \
  modelingevolution/blazor-perfmon-example:latest
```

**Docker Compose**: Add the tegrastats volume:

```yaml
volumes:
  - /var/run/docker.sock:/var/run/docker.sock:ro
  - /proc:/host/proc:ro
  - /usr/bin/tegrastats:/usr/bin/tegrastats:ro
environment:
  - MonitorSettings__GpuCollectorType=NvTegra
```

### NVIDIA Desktop GPUs (x64)

For NVIDIA desktop GPU monitoring, uncomment the GPU configuration in `docker-compose.yml`:

```yaml
deploy:
  resources:
    reservations:
      devices:
        - driver: nvidia
          count: all
          capabilities: [gpu, utility, compute]
```

You also need NVIDIA Container Toolkit installed:

```bash
# Install NVIDIA Container Toolkit
distribution=$(. /etc/os-release;echo $ID$VERSION_ID)
curl -s -L https://nvidia.github.io/nvidia-docker/gpgkey | sudo apt-key add -
curl -s -L https://nvidia.github.io/nvidia-docker/$distribution/nvidia-docker.list | \
  sudo tee /etc/apt/sources.list.d/nvidia-docker.list

sudo apt-get update
sudo apt-get install -y nvidia-container-toolkit
sudo systemctl restart docker
```

## Production Deployment

### Security Considerations

1. **Remove demo containers** from `docker-compose.yml`
2. **Use specific user** instead of root (already configured)
3. **Limit capabilities** to minimum required
4. **Use secrets** for sensitive configuration
5. **Enable TLS** for production

### Example Production Configuration

```yaml
services:
  perfmon:
    image: modelingevolution/blazor-perfmon-example:v1.0.0
    restart: always
    ports:
      - "443:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=https://+:8080
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/app/cert.pfx
      - ASPNETCORE_Kestrel__Certificates__Default__Password=${CERT_PASSWORD}
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock:ro
      - /proc:/host/proc:ro
      - ./cert.pfx:/app/cert.pfx:ro
      - ./appsettings.Production.json:/app/appsettings.json:ro
    cap_add:
      - SYS_ADMIN
      - NET_ADMIN
    security_opt:
      - no-new-privileges:true
    networks:
      - perfmon-network
```

## Monitoring the Container

### View Logs

```bash
docker-compose logs -f perfmon
```

### Health Check

```bash
curl http://localhost:5000/health
```

### Container Stats

```bash
docker stats perfmon-example
```

## Troubleshooting

### Permission Denied for Docker Socket

Ensure the Docker socket is readable:

```bash
sudo chmod 666 /var/run/docker.sock
```

Or add the container user to the docker group (less secure).

### Cannot Read /proc Files

The container needs `SYS_ADMIN` capability:

```yaml
cap_add:
  - SYS_ADMIN
```

### GPU Not Detected

1. Verify NVIDIA Container Toolkit is installed
2. Check GPU is accessible: `nvidia-smi`
3. Verify runtime configuration in `docker-compose.yml`

### Port Already in Use

Change the port mapping in `docker-compose.yml`:

```yaml
ports:
  - "5001:8080"  # Use port 5001 instead of 5000
```

## Advanced Usage

### Custom Network Interface Monitoring

Edit `appsettings.json` or use environment variables:

```yaml
environment:
  - MonitorSettings__NetworkInterface=enp0s1,wlan0
```

### Custom Disk Monitoring

```yaml
environment:
  - MonitorSettings__DiskDevice=nvme0n1
```

### Adjust Collection Interval

```yaml
environment:
  - MonitorSettings__CollectionIntervalMs=1000  # 1 second
```

## Development

### Live Debugging

For development with live debugging, mount the source code:

```yaml
volumes:
  - ../../src:/src
  - /var/run/docker.sock:/var/run/docker.sock:ro
  - /proc:/host/proc:ro
```

Then use `dotnet watch` inside the container.

## Additional Resources

- [Main README](../../README.md)
- [NVIDIA Container Toolkit](https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/install-guide.html)
- [Docker Multi-Platform Builds](https://docs.docker.com/build/building/multi-platform/)
- [ASP.NET Core Docker Guide](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/)
