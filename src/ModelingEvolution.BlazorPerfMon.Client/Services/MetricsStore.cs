using Frontend.Models;

namespace Frontend.Services;

/// <summary>
/// Manages circular buffers for metrics data with 60-second rolling window.
/// Stage 4: CPU, GPU, Network, and Disk metrics.
/// </summary>
public sealed class MetricsStore
{
    private const int DataPoints = 120; // 60 seconds at 2Hz

    // Timestamp buffer (shared across all metrics)
    private readonly CircularBuffer<uint> _timestampBuffer;

    // Stage 1: CPU buffers (one per core, dynamically sized)
    private CircularBuffer<float>[] _cpuBuffers;

    // Total average CPU load over time
    private readonly CircularBuffer<float> _totalAverageBuffer;

    // Stage 4: GPU buffer
    private readonly CircularBuffer<float> _gpuBuffer;

    // Stage 2: Network buffers
    private readonly CircularBuffer<ulong> _networkRxBuffer;
    private readonly CircularBuffer<ulong> _networkTxBuffer;

    // Stage 3: Disk buffers
    private readonly CircularBuffer<ulong> _diskReadBytesBuffer;
    private readonly CircularBuffer<ulong> _diskWriteBytesBuffer;
    private readonly CircularBuffer<uint> _diskReadIopsBuffer;
    private readonly CircularBuffer<uint> _diskWriteIopsBuffer;

    /// <summary>
    /// Event fired when new metrics are added to the store.
    /// </summary>
    public event Action? OnMetricsUpdated;

    public MetricsStore()
    {
        // Start with empty array, will be initialized on first snapshot
        _timestampBuffer = new CircularBuffer<uint>(DataPoints);
        _cpuBuffers = Array.Empty<CircularBuffer<float>>();
        _totalAverageBuffer = new CircularBuffer<float>(DataPoints);
        _gpuBuffer = new CircularBuffer<float>(DataPoints);
        _networkRxBuffer = new CircularBuffer<ulong>(DataPoints);
        _networkTxBuffer = new CircularBuffer<ulong>(DataPoints);
        _diskReadBytesBuffer = new CircularBuffer<ulong>(DataPoints);
        _diskWriteBytesBuffer = new CircularBuffer<ulong>(DataPoints);
        _diskReadIopsBuffer = new CircularBuffer<uint>(DataPoints);
        _diskWriteIopsBuffer = new CircularBuffer<uint>(DataPoints);
    }

    /// <summary>
    /// Add a metrics snapshot to the store.
    /// Stage 4: Processes CPU, GPU, Network, and Disk data.
    /// </summary>
    public void AddSnapshot(MetricsSnapshot snapshot)
    {
        // Store timestamp
        _timestampBuffer.Add(snapshot.TimestampMs);

        // Stage 1: Store CPU metrics
        if (snapshot.CpuLoads != null && snapshot.CpuLoads.Length > 0)
        {
            // Initialize buffers on first snapshot
            if (_cpuBuffers.Length != snapshot.CpuLoads.Length)
            {
                _cpuBuffers = new CircularBuffer<float>[snapshot.CpuLoads.Length];
                for (int i = 0; i < snapshot.CpuLoads.Length; i++)
                {
                    _cpuBuffers[i] = new CircularBuffer<float>(DataPoints);
                }
            }

            for (int i = 0; i < snapshot.CpuLoads.Length; i++)
            {
                _cpuBuffers[i].Add(snapshot.CpuLoads[i]);
            }

            // Calculate and store total average
            float average = snapshot.CpuLoads.Average();
            _totalAverageBuffer.Add(average);
        }

        // Stage 4: Store GPU metrics
        _gpuBuffer.Add(snapshot.GpuLoad);

        // Stage 2: Store Network metrics
        _networkRxBuffer.Add(snapshot.NetworkRxBytes);
        _networkTxBuffer.Add(snapshot.NetworkTxBytes);

        // Stage 3: Store Disk metrics
        _diskReadBytesBuffer.Add(snapshot.DiskReadBytes);
        _diskWriteBytesBuffer.Add(snapshot.DiskWriteBytes);
        _diskReadIopsBuffer.Add(snapshot.DiskReadIops);
        _diskWriteIopsBuffer.Add(snapshot.DiskWriteIops);

        OnMetricsUpdated?.Invoke();
    }

    /// <summary>
    /// Get CPU data for a specific core.
    /// </summary>
    public CircularBuffer<float> GetCpuBuffer(int coreIndex)
    {
        if (coreIndex < 0 || coreIndex >= _cpuBuffers.Length)
            throw new ArgumentOutOfRangeException(nameof(coreIndex));

        return _cpuBuffers[coreIndex];
    }

    /// <summary>
    /// Get all CPU buffers (dynamically sized based on detected cores).
    /// </summary>
    public CircularBuffer<float>[] GetAllCpuBuffers() => _cpuBuffers;

    /// <summary>
    /// Get the number of CPU cores detected.
    /// </summary>
    public int CoreCount => _cpuBuffers.Length;

    /// <summary>
    /// Get timestamp buffer (Unix timestamp in milliseconds).
    /// </summary>
    public CircularBuffer<uint> GetTimestampBuffer() => _timestampBuffer;

    /// <summary>
    /// Get total average CPU load buffer.
    /// </summary>
    public CircularBuffer<float> GetTotalAverageBuffer() => _totalAverageBuffer;

    /// <summary>
    /// Get GPU utilization buffer.
    /// </summary>
    public CircularBuffer<float> GetGpuBuffer() => _gpuBuffer;

    /// <summary>
    /// Get network Rx buffer.
    /// </summary>
    public CircularBuffer<ulong> GetNetworkRxBuffer() => _networkRxBuffer;

    /// <summary>
    /// Get network Tx buffer.
    /// </summary>
    public CircularBuffer<ulong> GetNetworkTxBuffer() => _networkTxBuffer;

    /// <summary>
    /// Get disk read bytes buffer.
    /// </summary>
    public CircularBuffer<ulong> GetDiskReadBytesBuffer() => _diskReadBytesBuffer;

    /// <summary>
    /// Get disk write bytes buffer.
    /// </summary>
    public CircularBuffer<ulong> GetDiskWriteBytesBuffer() => _diskWriteBytesBuffer;

    /// <summary>
    /// Get disk read IOPS buffer.
    /// </summary>
    public CircularBuffer<uint> GetDiskReadIopsBuffer() => _diskReadIopsBuffer;

    /// <summary>
    /// Get disk write IOPS buffer.
    /// </summary>
    public CircularBuffer<uint> GetDiskWriteIopsBuffer() => _diskWriteIopsBuffer;

    /// <summary>
    /// Clear all stored metrics.
    /// </summary>
    public void Clear()
    {
        _timestampBuffer.Clear();

        foreach (var buffer in _cpuBuffers)
        {
            buffer.Clear();
        }

        _totalAverageBuffer.Clear();
        _gpuBuffer.Clear();
        _networkRxBuffer.Clear();
        _networkTxBuffer.Clear();
        _diskReadBytesBuffer.Clear();
        _diskWriteBytesBuffer.Clear();
        _diskReadIopsBuffer.Clear();
        _diskWriteIopsBuffer.Clear();

        OnMetricsUpdated?.Invoke();
    }
}
