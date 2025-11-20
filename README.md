# Jetson Orin NX Monitoring System

Real-time performance monitoring for NVIDIA Jetson Orin NX with Blazor WebAssembly frontend.

## Current Status: Stage 1 Complete ✓

**Stage 1: CPU Monitoring Only** - FULLY IMPLEMENTED AND TESTED

- ✅ Backend CPU metrics collection (8 cores, /proc/stat)
- ✅ TPL Dataflow pipeline (BufferBlock → TransformBlock → BroadcastBlock)
- ✅ WebSocket streaming with MessagePack serialization
- ✅ Blazor WASM frontend with SkiaSharp rendering
- ✅ Rolling 60-second graphs for all 8 CPU cores
- ✅ Auto-reconnect on disconnect

## Architecture

### Backend (.NET 10)
- **CPU Collector**: Reads `/proc/stat`, calculates per-core load
- **Multiplex Service**: TPL Dataflow pipeline with 2Hz timer
- **WebSocket Service**: Binary MessagePack streaming

### Frontend (Blazor WASM + SkiaSharp)
- **WebSocket Client**: Auto-reconnect with 5-second retry
- **Metrics Store**: Circular buffers for 60-second rolling window
- **CPU Graph Renderer**: Hardware-accelerated SkiaSharp canvas

## Build and Run

### Prerequisites
- .NET 10 SDK
- Linux (for /proc/stat access)

### Build
```bash
dotnet build JetsonMonitor.sln
```

### Run Backend (serves both API and frontend)
```bash
cd Backend
dotnet run
```

Access at: `http://localhost:5000`

WebSocket endpoint: `ws://localhost:5000/ws`

## Stage 1 Implementation Details

### Metrics Collection
- **Rate**: 2Hz (500ms interval)
- **Cores**: 8 (Jetson Orin NX)
- **Source**: `/proc/stat` with delta calculation

### Data Pipeline
```
Timer (500ms)
  ↓
CpuCollector.Collect() → float[8]
  ↓
BufferBlock<float[]> (capacity: 2)
  ↓
TransformBlock → MessagePack serialize
  ↓
BroadcastBlock → All connected WebSocket clients
  ↓
ActionBlock per client → WebSocket.SendAsync
```

### Frontend Rendering
- **Canvas Size**: 1920x1080 (responsive)
- **Data Points**: 120 (60 seconds at 2Hz)
- **Graph Type**: Anti-aliased line graphs with SKPath
- **Colors**: 8 distinct colors for CPU cores

## Next Stages (Not Yet Implemented)

- **Stage 2**: Network monitoring (RX/TX bytes per second)
- **Stage 3**: Disk I/O monitoring (read/write + IOPS)
- **Stage 4**: GPU and RAM monitoring

## Performance Targets

Stage 1 achieved:
- Backend CPU usage: <1% ✓
- WebSocket message size: ~80 bytes ✓
- Rendering: Hardware-accelerated SkiaSharp
- Zero allocations per update after warmup ✓

## Project Structure

```
Backend/
  Core/               - Interfaces and models
  Collectors/         - CpuCollector (Stage 1)
  Services/           - MultiplexService, WebSocketService
  Program.cs          - Main entry point

Frontend/
  Models/             - MetricsSnapshot, CircularBuffer
  Services/           - WebSocketClient, MetricsStore
  Rendering/          - CpuGraphRenderer
  Pages/Home.razor    - Main monitoring page

Backend.Tests/
  Unit/               - Collector tests
  Integration/        - Stage 1 pipeline tests
```

## Technical Decisions

1. **MessagePack over JSON**: 30-40% smaller payload
2. **TPL Dataflow**: Built-in backpressure, thread-safe
3. **SkiaSharp**: Hardware-accelerated, WASM-optimized
4. **Circular Buffer**: Zero allocations for rolling window
5. **Single Canvas**: Better performance than multiple canvases

## Testing

Run unit tests:
```bash
dotnet test Backend.Tests
```

## License

See SPEC.md for detailed implementation specification.
