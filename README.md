# Blazor Performance Monitor

[![NuGet Server](https://img.shields.io/nuget/v/ModelingEvolution.PerformanceMonitor.Server.svg)](https://www.nuget.org/packages/ModelingEvolution.PerformanceMonitor.Server/)
[![NuGet Client](https://img.shields.io/nuget/v/ModelingEvolution.PerformanceMonitor.Client.svg)](https://www.nuget.org/packages/ModelingEvolution.PerformanceMonitor.Client/)
[![NuGet Shared](https://img.shields.io/nuget/v/ModelingEvolution.PerformanceMonitor.Shared.svg)](https://www.nuget.org/packages/ModelingEvolution.PerformanceMonitor.Shared/)

Real-time comprehensive performance monitoring system with Blazor WebAssembly frontend and SkiaSharp rendering.

## Current Status: Multi-Metric Monitoring System ✓

**Comprehensive System Monitoring** - FULLY IMPLEMENTED

- ✅ CPU monitoring (multi-core bar charts and time-series)
- ✅ GPU monitoring (NVIDIA multi-GPU support with bar charts)
- ✅ **NVIDIA Jetson Tegra support** (tegrastats-based monitoring)
- ✅ **Temperature monitoring** (multi-sensor visualization with min/max tracking)
- ✅ Network monitoring (multi-interface RX/TX tracking)
- ✅ Disk I/O monitoring (read/write operations)
- ✅ Docker container monitoring (CPU % and memory usage with unified colors)
- ✅ RAM monitoring with time-series visualization
- ✅ **Client-server timestamp synchronization** (automatic clock skew correction)
- ✅ WebSocket streaming with MessagePack serialization
- ✅ Blazor WASM frontend with SkiaSharp hardware-accelerated rendering
- ✅ Immutable circular buffers for thread-safe data storage
- ✅ Auto-reconnect on disconnect
- ✅ **Unified color scheme** across all charts (RAM: blue, CPU: green)

## Architecture

### Backend (.NET 10)
- **Metrics Collectors**:
  - **CpuCollector**: Reads `/proc/stat`, calculates per-core load
  - **GpuCollector**: NVIDIA GPU monitoring via `nvidia-smi`
  - **NetworkCollector**: Multi-interface network statistics from `/proc/net/dev`
  - **DiskCollector**: Disk I/O metrics from `/proc/diskstats`
  - **DockerCollector**: Container metrics via Docker API
- **Multiplex Service**: TPL Dataflow pipeline with configurable timer
- **WebSocket Service**: Binary MessagePack streaming

### Frontend (Blazor WASM + SkiaSharp)
- **WebSocket Client**: Auto-reconnect with 5-second retry
- **Metrics Store**: ImmutableCircularBuffer for thread-safe 60-second rolling window
- **Chart Renderers**: Hardware-accelerated SkiaSharp canvas with multiple chart types:
  - **BarChart**: Generic bar chart with customizable colors
  - **CpuBarChart**: Per-core CPU load visualization
  - **GpuBarChart**: Per-GPU load visualization
  - **TimeSeriesChart**: Time-based line charts with dynamic scaling
  - **NetworkChart**: Network RX/TX time-series
  - **DiskChart**: Disk I/O time-series
  - **DockerContainersChart**: Double-bar visualization (memory + CPU)
  - **ComputeLoadChart**: Overall system compute load
- **Brushes**: Static reusable SKPaint objects to avoid allocations in hot rendering paths

## Build and Run

### Prerequisites
- .NET 10 SDK
- Linux (for /proc access to system metrics)
- NVIDIA GPU with `nvidia-smi` (optional, for GPU monitoring)
- Docker (optional, for container monitoring)

### Build
```bash
dotnet build BlazorPerfMon.sln
```

### Run
```bash
cd src/ModelingEvolution.BlazorPerfMon.Server
dotnet run
```

Access at: `http://localhost:5000`

WebSocket endpoint: `ws://localhost:5000/ws`

## Using the Client Library

### Blazor WebAssembly Setup

To integrate the Performance Monitor client into your Blazor WASM application:

1. **Install the NuGet package**:
```bash
dotnet add package ModelingEvolution.PerformanceMonitor.Client
```

2. **Register the client services** in `Program.cs`:
```csharp
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ModelingEvolution.BlazorPerfMon.Client.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Calculate WebSocket URL from base address
string wsUrl = builder.HostEnvironment.BaseAddress
    .Replace("http://", "ws://")
    .Replace("https://", "wss://") + "ws";

// Register Performance Monitor client
builder.Services.AddPerformanceMonitorClient(wsUrl, dataPointsToKeep: 120);

await builder.Build().RunAsync();
```

3. **Add the component** to your page:
```razor
@page "/"
@using ModelingEvolution.BlazorPerfMon.Client.Components

<PerformanceMonitorCanvas />
```

### Component Features

- **Automatic reconnection**: Component handles WebSocket disconnections and reconnects automatically
- **Component lifecycle**: Each component instance creates its own WebSocket connection and metrics store
- **Disposal support**: Components can be disposed and recreated without issues
- **Multiple instances**: Multiple monitor components can coexist in the same application

### Component Parameters

- `ServerUrl` (optional): Override the default WebSocket URL for this component instance
- `ShowStatusIndicator` (default: true): Show/hide the connection status indicator (green/yellow/red dot)

Example with custom parameters:
```razor
<PerformanceMonitorCanvas ServerUrl="ws://custom-server:5000/ws" ShowStatusIndicator="true" />
```

## Implementation Details

### Metrics Collection
- **Rate**: Configurable (default 500ms interval)
- **CPU**: Multi-core support with per-core load calculation
- **GPU**: NVIDIA multi-GPU support via nvidia-smi
- **Network**: Multi-interface support (configurable via settings)
- **Disk**: Multi-disk I/O monitoring
- **Docker**: Container-level CPU and memory tracking
- **Sources**: `/proc/stat`, `/proc/net/dev`, `/proc/diskstats`, nvidia-smi, Docker API

### Data Pipeline
```
Timer (configurable interval)
  ↓
Parallel Collectors → MetricSample
  ├─ CpuCollector.Collect() → float[]
  ├─ GpuCollector.Collect() → GpuMetric[]
  ├─ NetworkCollector.Collect() → NetworkMetric[]
  ├─ DiskCollector.Collect() → DiskMetric[]
  └─ DockerCollector.Collect() → DockerContainerMetric[]
  ↓
BufferBlock<MetricSample> (capacity: configurable)
  ↓
TransformBlock → MessagePack serialize
  ↓
BroadcastBlock → All connected WebSocket clients
  ↓
ActionBlock per client → WebSocket.SendAsync
```

### Frontend Rendering
- **Canvas**: Responsive sizing
- **Data Storage**: ImmutableCircularBuffer for thread-safe rolling window
- **Chart Types**: Bar charts, time-series line charts with fill
- **Performance Optimizations**:
  - Reusable Brushes (SKPaint objects)
  - Zero-allocation rendering after warmup
  - Hardware-accelerated SkiaSharp
  - Efficient time-based rendering with interpolation
  - Dynamic scaling with min/max tracking

## Performance Targets

Achieved:
- Backend CPU usage: <5% with all collectors enabled ✓
- WebSocket message size: Compact MessagePack binary format ✓
- Frontend rendering: Hardware-accelerated SkiaSharp ✓
- Zero allocations per render after warmup (via reusable Brushes) ✓
- Thread-safe data access without locks (via ImmutableCircularBuffer) ✓
- Smooth time-series rendering with interpolation ✓

## Project Structure

```
src/
  ModelingEvolution.BlazorPerfMon.Server/
    Core/                        - Interfaces (IMetricsCollector)
    Collectors/                  - All metrics collectors
      ├─ CpuCollector.cs        - CPU per-core load
      ├─ GpuCollector.cs        - NVIDIA GPU monitoring
      ├─ NetworkCollector.cs    - Network interface stats
      ├─ DiskCollector.cs       - Disk I/O metrics
      └─ DockerCollector.cs     - Container monitoring
    Services/                    - MultiplexService, WebSocketService
    Program.cs                   - Main entry point

  ModelingEvolution.BlazorPerfMon.Client/
    Models/                      - MetricSample, CircularBuffer
    Collections/                 - ImmutableCircularBuffer
    Services/                    - WebSocketClient, MetricsStore
    Rendering/                   - All chart renderers
      ├─ IChart.cs              - Chart interface
      ├─ Brushes.cs             - Reusable SKPaint objects
      ├─ BarChart.cs            - Generic bar chart
      ├─ CpuBarChart.cs         - CPU bar visualization
      ├─ GpuBarChart.cs         - GPU bar visualization
      ├─ TimeSeriesChart.cs     - Time-series line charts
      ├─ NetworkChart.cs        - Network RX/TX charts
      ├─ DiskChart.cs           - Disk I/O charts
      ├─ DockerContainersChart.cs - Docker visualization
      └─ ComputeLoadChart.cs    - Overall compute load
    Pages/                       - Blazor pages

  ModelingEvolution.BlazorPerfMon.Shared/
    Models/                      - Shared DTOs and models

examples/
  ModelingEvolution.BlazorPerfMon.Example/
                                - Example implementation
```

## Technical Decisions

1. **MessagePack over JSON**: 30-40% smaller payload for efficient binary streaming
2. **TPL Dataflow**: Built-in backpressure, thread-safe pipeline architecture
3. **SkiaSharp**: Hardware-accelerated, WASM-optimized 2D graphics
4. **ImmutableCircularBuffer**: Lock-free thread-safety via immutable collections
   - Eliminates lock contention between collection thread and UI render loop (60 FPS)
   - Trade-off: GC pressure from allocations vs. lock contention in hot path
5. **Reusable Brushes**: Static SKPaint objects to avoid allocations during rendering
6. **IChart Interface**: Uniform chart rendering at (0,0) with canvas transforms
7. **Time-based Rendering**: Charts render based on timestamps, not data points
8. **Smooth Interpolation**: Left/right edge clipping with interpolated boundaries
9. **Dynamic Scaling**: Automatic min/max calculation with 10% padding
10. **Multi-collector Architecture**: Parallel collection of different metric types

## Key Features

### Docker Container Monitoring
- Real-time container metrics (CPU %, memory usage)
- Double-bar visualization: memory bar with CPU overlay
- Min/Max RAM tracking per container with dotted lines
- Dynamic scaling based on system RAM
- Container name truncation for compact display

### Time-Series Charts
- Timestamp-based rendering (independent of data point count)
- Smooth left/right edge clipping with interpolation
- Dynamic Y-axis scaling with configurable ranges
- Fill gradients under line graphs
- Configurable time windows (default 60 seconds)

### Network Monitoring
- Multi-interface support (configurable)
- RX/TX bytes per second tracking
- Delta calculation for accurate throughput

### GPU Monitoring
- Multi-GPU support via NVIDIA nvidia-smi
- Per-GPU load percentage
- Temperature and memory tracking

## Testing

Run tests:
```bash
dotnet test
```

## Configuration

Configure monitoring in `appsettings.json` or `appsettings.{environment}.json`:

### MonitorSettings

```json
{
  "MonitorSettings": {
    "NetworkInterface": "eth0",
    "DiskDevice": "sda",
    "CollectionIntervalMs": 500,
    "DataPointsToKeep": 120,
    "GpuCollectorType": "NvSmi",
    "Layout": [
      [ "CPU/16", "GPU/1", "Docker", "ComputeLoad/3|col-span:2" ],
      [ "Network:eth0/2", "Disk:sda/2" ]
    ]
  }
}
```

### Configuration Options

- **NetworkInterface**: Network interface to monitor (e.g., `eth0`, `ether4`)
- **DiskDevice**: Disk device to monitor (e.g., `sda`, `mmcblk0`)
- **CollectionIntervalMs**: Metrics collection interval in milliseconds (default: 500)
- **DataPointsToKeep**: Number of data points to retain in rolling window (default: 120)
- **GpuCollectorType**: GPU collector implementation
  - `NvSmi`: Standard NVIDIA GPU monitoring via nvidia-smi
  - `NvTegra`: NVIDIA Jetson Tegra monitoring via tegrastats
- **Layout**: Grid layout configuration (see Layout Syntax below)

### Layout Syntax

The Layout property defines how charts are arranged in a responsive grid. Each row is an array of chart specifications.

**Chart Specification Format:**
```
ChartType[:Identifier]/Count[|col-span:N]
```

**Components:**
- `ChartType`: Type of chart (CPU, GPU, RAM, Network, Disk, Docker, ComputeLoad, Temperature)
- `:Identifier`: Optional identifier for the specific instance (e.g., network interface name, disk device)
- `/Count`: Number of data points for this metric (e.g., 16 CPU cores, 2 GPUs)
- `|col-span:N`: Optional column span for proportional width (default: 1, range: 1-12)

**Examples:**
- `"CPU/8"` - CPU chart with 8 cores, spans 1 column
- `"Network:eth0/2"` - Network chart for eth0 interface with 2 data points (RX/TX)
- `"Disk:sda/2"` - Disk chart for sda device with 2 data points (read/write)
- `"ComputeLoad/3|col-span:2"` - Compute load chart with 3 data points, spans 2 columns
- `"Temperature/9|col-span:5"` - Temperature chart with 9 sensors, spans 5 columns

**Layout Calculation:**
- Each row's total width = sum of all col-span values in that row
- Chart proportional width = col-span / total row width
- Example: `["CPU/8", "Docker", "ComputeLoad/3|col-span:2"]`
  - Total: 1 + 1 + 2 = 4
  - Widths: CPU=1/4 (25%), Docker=1/4 (25%), ComputeLoad=2/4 (50%)

### Example: NVIDIA Jetson Tegra Configuration

```json
{
  "MonitorSettings": {
    "NetworkInterface": "ether4",
    "DiskDevice": "mmcblk0",
    "CollectionIntervalMs": 500,
    "DataPointsToKeep": 120,
    "GpuCollectorType": "NvTegra",
    "Layout": [
      [ "CPU/8", "Docker", "ComputeLoad/3|col-span:2" ],
      [ "Network:ether4/2", "Disk:mmcblk0/2" ],
      [ "Temperature/9|col-span:5" ]
    ]
  }
}
```

### Example: Standard x86 Configuration

```json
{
  "MonitorSettings": {
    "NetworkInterface": "eth0",
    "DiskDevice": "sda",
    "CollectionIntervalMs": 500,
    "DataPointsToKeep": 120,
    "GpuCollectorType": "NvSmi",
    "Layout": [
      [ "CPU/16", "GPU/1", "Docker", "ComputeLoad/3|col-span:2" ],
      [ "Network:eth0/2", "Disk:sda/2" ]
    ]
  }
}
```

## Publishing NuGet Packages

The project is set up to automatically publish NuGet packages to NuGet.org via GitHub Actions.

### Quick Release

Use the provided release script:

```bash
./release.sh 1.0.0
```

This will:
1. Create a git tag `perfmon/1.0.0`
2. Push the tag to GitHub
3. Trigger automatic NuGet package publishing

### Manual Release

Alternatively, create and push a tag manually:

```bash
git tag perfmon/1.0.0
git push origin perfmon/1.0.0
```

### Published Packages

- **ModelingEvolution.PerformanceMonitor.Shared** - Shared models and DTOs
- **ModelingEvolution.PerformanceMonitor.Server** - Server-side metrics collection
- **ModelingEvolution.PerformanceMonitor.Client** - Blazor WASM client with SkiaSharp rendering

## License

See SPEC.md for detailed implementation specification.
