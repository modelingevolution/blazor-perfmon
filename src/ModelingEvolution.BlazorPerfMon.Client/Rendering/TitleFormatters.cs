using ModelingEvolution.BlazorPerfMon.Client.Extensions;
using ModelingEvolution.BlazorPerfMon.Shared;

namespace ModelingEvolution.BlazorPerfMon.Client.Rendering;

/// <summary>
/// Value object representing compute load title data.
/// Uses epsilon-based equality to avoid reformatting on tiny value changes.
/// </summary>
internal struct ComputeLoadTitleData : IEquatable<ComputeLoadTitleData>
{
    public float CpuAvg;
    public float GpuAvg;
    public Bytes RamUsed;
    public Bytes RamTotal;

    // Epsilon comparison - only reformat if values change by >= 0.1%
    public bool Equals(ComputeLoadTitleData other) =>
        MathF.Abs(CpuAvg - other.CpuAvg) < 0.1f &&
        MathF.Abs(GpuAvg - other.GpuAvg) < 0.1f &&
        RamUsed.Equals(other.RamUsed) &&
        RamTotal.Equals(other.RamTotal);

    public override bool Equals(object? obj) => obj is ComputeLoadTitleData other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(CpuAvg, GpuAvg, RamUsed, RamTotal);
}

/// <summary>
/// Formatter for compute load chart titles with caching.
/// Only allocates new string when values change beyond epsilon threshold.
/// </summary>
internal sealed class ComputeLoadTitleFormatter
{
    private ComputeLoadTitleData _lastData;
    private string _cached = string.Empty;

    public string Format(ComputeLoadTitleData current)
    {
        if (!current.Equals(_lastData))
        {
            _cached = $"Compute Load {current.CpuAvg:F1}% CPU, {current.GpuAvg:F1}% GPU, {current.RamUsed}/{current.RamTotal} RAM";
            _lastData = current;
        }
        return _cached;
    }
}

/// <summary>
/// Value object representing network chart title data.
/// </summary>
internal struct NetworkTitleData : IEquatable<NetworkTitleData>
{
    public string InterfaceName;
    public Bytes RxBytes;
    public Bytes TxBytes;

    // Bytes already has value-based equality
    public bool Equals(NetworkTitleData other) =>
        InterfaceName == other.InterfaceName &&
        RxBytes.Equals(other.RxBytes) &&
        TxBytes.Equals(other.TxBytes);

    public override bool Equals(object? obj) => obj is NetworkTitleData other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(InterfaceName, RxBytes, TxBytes);
}

/// <summary>
/// Formatter for network chart titles with caching.
/// </summary>
internal sealed class NetworkTitleFormatter
{
    private NetworkTitleData _lastData;
    private string _cached = string.Empty;

    public string Format(NetworkTitleData current)
    {
        if (!current.Equals(_lastData))
        {
            _cached = $"Network {current.InterfaceName} {current.RxBytes.FormatFixed()}/s RX, {current.TxBytes.FormatFixed()}/s TX";
            _lastData = current;
        }
        return _cached;
    }
}

/// <summary>
/// Value object representing disk chart title data.
/// </summary>
internal struct DiskTitleData : IEquatable<DiskTitleData>
{
    public string DiskDevice;
    public Bytes ReadBytes;
    public Bytes WriteBytes;

    public bool Equals(DiskTitleData other) =>
        DiskDevice == other.DiskDevice &&
        ReadBytes.Equals(other.ReadBytes) &&
        WriteBytes.Equals(other.WriteBytes);

    public override bool Equals(object? obj) => obj is DiskTitleData other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(DiskDevice, ReadBytes, WriteBytes);
}

/// <summary>
/// Formatter for disk chart titles with caching.
/// </summary>
internal sealed class DiskTitleFormatter
{
    private DiskTitleData _lastData;
    private string _cached = string.Empty;

    public string Format(DiskTitleData current)
    {
        if (!current.Equals(_lastData))
        {
            _cached = $"Disk {current.DiskDevice} {current.ReadBytes.FormatFixed()}/s Read, {current.WriteBytes.FormatFixed()}/s Write";
            _lastData = current;
        }
        return _cached;
    }
}


