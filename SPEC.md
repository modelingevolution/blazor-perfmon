# Jetson Orin NX Monitoring System - Implementation Specification

## Current Stage: Stage 1 - CPU Monitoring Only

### Implementation Order (STRICT)
1. **Stage 1**: CPU monitoring only - MUST be fully working before Stage 2
2. **Stage 2**: Add Network monitoring - MUST be fully working before Stage 3
3. **Stage 3**: Add Disk I/O monitoring - MUST be fully working before Stage 4
4. **Stage 4**: Add GPU and RAM monitoring - Complete system

---

## Compiler Settings (MANDATORY FOR ALL PROJECTS)

```xml
<!-- Required in Backend.csproj, Frontend.csproj, Backend.Tests.csproj -->
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <WarningLevel>9999</WarningLevel>
  <NoWarn></NoWarn>  <!-- NO suppressions allowed -->
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  <AnalysisLevel>latest-all</AnalysisLevel>
</PropertyGroup>
```

**Enforcement Rules:**
- Build MUST fail on ANY warning
- NO `#pragma warning disable` allowed
- NO `SuppressMessage` attributes allowed
- Nullable reference warnings are errors
- Code style violations are errors
- ALL warnings from ALL analyzers are errors

---

## Stage 1: CPU Monitoring Only

### Goals
- Implement CPU metrics collection for 8 cores
- Complete backend pipeline with single metric type
- SkiaSharp rolling graph displaying all 8 cores
- Full integration tests for CPU pipeline
- **Must be fully working before proceeding to Stage 2**

### Backend Components (Stage 1)

#### Data Collection
- **CPU Metrics**: 8 cores, `core_load_percent` (float32, 0-100)
- **Source**: `/proc/stat`, calculate delta between reads
- **Collection Rate**: Every 500ms (2Hz)

#### Memory Management Requirements
- **ArrayPool Usage**: ALL byte arrays MUST use `ArrayPool<byte>.Shared`
- **Zero Allocations**: After warmup, no allocations per update cycle
- **Object Pooling**: Pool SKPaint, SKPath, and other SkiaSharp objects
- **Buffer Reuse**: Reuse MessagePack serialization buffers

#### TPL Dataflow Pipeline (Stage 1)
```csharp
// Required configuration for ALL DataflowBlockOptions:
var blockOptions = new DataflowBlockOptions { 
    BoundedCapacity = 2  // Minimal buffering
};

var executionOptions = new ExecutionDataflowBlockOptions {
    BoundedCapacity = 2,
    MaxMessagesPerTask = 1,           // Required: No batching
    SingleProducerConstrained = true   // Required: ARM optimization
};

BufferBlock<float[]> cpuBuffer = new(blockOptions);
  ↓
TransformBlock<float[], byte[]> serializeBlock = new(
    cpuData => {
        // Required: Use ArrayPool<byte>.Shared
        var buffer = ArrayPool<byte>.Shared.Rent(512);
        try {
            // MessagePack serialization
            return MessagePackSerializer.Serialize(snapshot);
        } finally {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    },
    executionOptions)
  ↓
BroadcastBlock<byte[]> broadcastBlock = new(
    bytes => bytes,  // Clone function
    blockOptions)
  ↓
ActionBlock<byte[]> clientSender = new(  // Per WebSocket client
    async bytes => await webSocket.SendAsync(bytes, ...),
    new ExecutionDataflowBlockOptions {
        BoundedCapacity = 1,
        MaxDegreeOfParallelism = 1
    });
```

#### MessagePack Model (All Stages)
```csharp
[MessagePackObject]
public sealed class MetricsSnapshot
{
    [Key(0)] public uint TimestampMs { get; init; }
    [Key(1)] public float GpuLoad { get; init; }              // Stage 4
    [Key(2)] public float[] CpuLoads { get; init; }           // Stage 1 ✓
    [Key(3)] public ulong NetworkRxBytes { get; init; }       // Stage 2
    [Key(4)] public ulong NetworkTxBytes { get; init; }       // Stage 2
    [Key(5)] public float RamPercent { get; init; }           // Stage 4
    [Key(6)] public ulong DiskReadBytes { get; init; }        // Stage 3
    [Key(7)] public ulong DiskWriteBytes { get; init; }       // Stage 3
    [Key(8)] public uint DiskReadIops { get; init; }          // Stage 3
    [Key(9)] public uint DiskWriteIops { get; init; }         // Stage 3
}
```
**Stage 1**: Only `TimestampMs` and `CpuLoads` are populated, others are 0/null

### Frontend Components (Stage 1)

#### UI Layout
- **Single SkiaSharp canvas** (1920x1080 responsive) - NO multiple canvases
- CPU graph takes full canvas height
- Display 8 cores with different colors
- 60-second rolling window (120 data points at 2Hz)

#### Rendering Requirements
- Anti-aliased SKPath drawing
- Update at 2Hz synchronized with backend
- CircularBuffer for efficient data storage (120 points per metric)
- Pre-calculated scales and transforms
- Object pooling for SKPaint/SKPath
- **Dirty Rectangle Tracking**: Only redraw changed regions
- **RequestAnimationFrame Sync**: Smooth 60fps capability
- **InvalidateSurface()** only on data arrival (2Hz)

### NuGet Packages (Stage 1)

**Backend:**
- `MessagePack` (v2.5+)
- `System.Threading.Tasks.Dataflow` (v8.0+)

**Frontend:**
- `MessagePack` (v2.5+)
- `SkiaSharp.Views.Blazor` (v2.88+)
- `SkiaSharp.WebAssembly` (v2.88+)

### Success Criteria (Stage 1)
- ✓ CPU graph displays 8 cores correctly
- ✓ 2Hz updates verified (±50ms tolerance)
- ✓ Backend CPU usage <1%
- ✓ Integration tests pass for CPU pipeline
- ✓ WebSocket stays connected for >10 minutes
- ✓ Zero memory allocations per update after warmup
- ✓ WebSocket message size <500 bytes
- ✓ Frontend rendering <5ms per frame

---

## Future Stages (Not Yet Implemented)

### Stage 2: Network Monitoring
- Add `NetworkCollector.cs` reading `/sys/class/net/{interface}/statistics/`
- Extend pipeline: `JoinBlock<float[], (ulong, ulong)>`
- Add network graph to canvas (50% height for CPU, 50% for network)
- Environment variable: `MONITOR_INTERFACE` (default: "eth0")
- **Success Criteria**: Backend CPU usage <1.5%

### Stage 3: Disk I/O Monitoring
- Add `DiskCollector.cs` reading `/proc/diskstats` for `/dev/nvme0n1`
- Extend pipeline: `JoinBlock<float[], (ulong, ulong), DiskMetrics>`
- Add disk I/O graphs (read/write + IOPS)
- Canvas split: CPU 33%, Network 33%, Disk 33%
- **Success Criteria**: Backend CPU usage <2%

### Stage 4: GPU and RAM Monitoring
- Add `GpuCollector.cs` (nvidia-smi dmon) and `MemoryCollector.cs` (/proc/meminfo)
- Complete pipeline: `JoinBlock<float, float[], (ulong, ulong), float, DiskMetrics>`
- Canvas split: GPU 25%, CPU 25%, Network 25%, Disk 25%
- **Success Criteria**: Complete system <2% CPU, <80MB RAM

---

## Technical Constraints

### Fixed Configuration
- Backend Port: 5000
- Frontend Port: 5001
- WebSocket: WS only (no WSS)
- Update Rate: 2Hz (500ms)
- Rolling Window: 60 seconds (120 data points)
- DataflowBlockOptions: BoundedCapacity 1-2
- SingleProducerConstrained: true (REQUIRED)
- MaxMessagesPerTask: 1 (REQUIRED)

### Performance Targets
- Backend CPU usage: <2% (Stage 4 complete)
- Frontend rendering: <5ms per frame (>200 FPS capability)
- Total RAM usage: <80MB
- WebSocket message: <500 bytes
- Unit test coverage: >80% for calculation logic
- Zero memory allocations after warmup

### Integration Test Requirements
- **Stage1_CpuOnlyTests.cs**: Full pipeline test with CPU only
- **Stage2_CpuNetworkTests.cs**: Test CPU + Network integration
- **Stage3_CpuNetDiskTests.cs**: Test CPU + Network + Disk
- **Stage4_FullSystemTests.cs**: All metrics integrated
- **WebSocketIntegrationTests.cs**: End-to-end WebSocket flow
- **PerformanceTests.cs**: Verify <2% CPU, <80MB RAM, <500 byte messages

### Out of Scope (DO NOT IMPLEMENT)
- Authentication/security
- Data persistence
- Configuration files (except MONITOR_INTERFACE env var)
- Multiple network interfaces (one at a time)
- GPU temperature/memory/power
- CPU frequency/temperature
- Historical data beyond 60 seconds
- REST API endpoints
- Health check endpoints
- Compression beyond MessagePack
- Logging framework (Console.WriteLine only for errors)
- Docker/containerization
- Multiple SKCanvases (single canvas only)
- Skipping stages (must implement in order)

---

## File Structure

```
/Backend
  /Core
    IMetricsCollector.cs     [Stage 1]
    MetricsSnapshot.cs       [Stage 1]
  /Collectors
    CpuCollector.cs          [Stage 1]
    NetworkCollector.cs      [Stage 2]
    DiskCollector.cs         [Stage 3]
    GpuCollector.cs          [Stage 4]
    MemoryCollector.cs       [Stage 4]
  /Services
    MultiplexService.cs      [Stage 1+]
    WebSocketService.cs      [Stage 1+]
    MetricsAggregator.cs     [Stage 1+]
  Program.cs                 [Stage 1]

/Frontend
  /Components
    RollingGraph.cs          [Stage 1]
    CpuGraph.razor           [Stage 1]
    NetworkGraph.razor       [Stage 2]
    DiskGraph.razor          [Stage 3]
    GpuGraph.razor           [Stage 4]
    CanvasRenderer.razor     [Stage 1]
  /Services
    WebSocketClient.cs       [Stage 1]
    MetricsDecoder.cs        [Stage 1]
    MetricsStore.cs          [Stage 1]
  /Models
    CircularBuffer.cs        [Stage 1]
  /Rendering
    CpuGraphRenderer.cs      [Stage 1]
  Pages/Index.razor          [Stage 1]

/Backend.Tests
  /Unit
    /Collectors
      CpuCollectorTests.cs   [Stage 1]
  /Integration
    Stage1_CpuOnlyTests.cs   [Stage 1]
    Stage2_CpuNetworkTests.cs [Stage 2]
    Stage3_CpuNetDiskTests.cs [Stage 3]
    Stage4_FullSystemTests.cs [Stage 4]
    WebSocketIntegrationTests.cs [Stage 1+]
    PerformanceTests.cs      [Stage 1+]
```

---

## Implementation Checklist (Stage 1)

### Compiler Configuration
- [ ] Add TreatWarningsAsErrors to Backend.csproj
- [ ] Add TreatWarningsAsErrors to Frontend.csproj
- [ ] Add TreatWarningsAsErrors to Backend.Tests.csproj
- [ ] Verify WarningLevel=9999 in all projects
- [ ] Ensure NoWarn is empty (no suppressions)

### Backend Implementation
- [ ] Backend Core interfaces with proper nullability
- [ ] CpuCollector with /proc/stat parsing
- [ ] MessagePack MetricsSnapshot model
- [ ] TPL Dataflow pipeline with required options
- [ ] ArrayPool usage in serialization
- [ ] WebSocket service with ActionBlock per client
- [ ] Backend Program.cs with 2Hz timer
- [ ] Zero allocation verification

### Frontend Implementation
- [ ] Frontend WebSocketClient with auto-reconnect
- [ ] CircularBuffer implementation (120 points)
- [ ] SkiaSharp CPU graph rendering
- [ ] Single SKCanvas enforcement
- [ ] Dirty rectangle tracking
- [ ] Object pooling for SKPaint/SKPath
- [ ] Main Index.razor with canvas
- [ ] 2Hz update synchronization

### Testing
- [ ] Unit tests for CPU collector
- [ ] Integration tests for Stage 1
- [ ] Performance tests (<1% CPU, <500 byte messages)
- [ ] End-to-end testing (WebSocket + rendering)
- [ ] 2Hz timing verification (±50ms)
