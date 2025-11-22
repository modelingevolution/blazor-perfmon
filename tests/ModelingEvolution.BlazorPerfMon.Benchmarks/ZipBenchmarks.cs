using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ModelingEvolution.BlazorPerfMon.Client.Collections;
using ModelingEvolution.BlazorPerfMon.Shared;

namespace ModelingEvolution.BlazorPerfMon.Benchmarks;

/// <summary>
/// Benchmarks comparing LINQ Zip vs custom zero-allocation ZipValues.
/// Run with: dotnet run -c Release --project tests/ModelingEvolution.BlazorPerfMon.Benchmarks
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class ZipBenchmarks
{
    private ImmutableCircularBuffer<MetricSample> _buffer = null!;
    private SampleAccessor<float> _floatAccessor = null!;
    private SampleAccessor<uint> _uintAccessor = null!;
    private List<float> _floatList = null!;
    private List<uint> _uintList = null!;

    [Params(10, 100, 500)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        // Create sample data
        _buffer = new ImmutableCircularBuffer<MetricSample>(N);
        for (int i = 0; i < N; i++)
        {
            var sample = new MetricSample
            {
                CreatedAt = (uint)(1000 + i),
                CpuLoads = new[] { 50f + i },
                GpuLoads = new[] { 30f + i },
                Ram = new RamMetric { UsedBytes = 1000, TotalBytes = 2000 },
                CpuAverage = 50f + i,
                GpuAverage = 30f + i
            };
            _buffer = _buffer.Add(sample);
        }

        // Create accessors
        _floatAccessor = new SampleAccessor<float>(_buffer, s => s.CpuAverage);
        _uintAccessor = new SampleAccessor<uint>(_buffer, s => s.CreatedAt);

        // Create lists for IEnumerable comparison
        _floatList = new List<float>();
        _uintList = new List<uint>();
        foreach (var sample in _buffer)
        {
            _floatList.Add(sample.CpuAverage);
            _uintList.Add(sample.CreatedAt);
        }
    }

    /// <summary>
    /// Baseline: LINQ Zip with IEnumerable (allocates enumerators and combined tuples)
    /// </summary>
    [Benchmark(Baseline = true)]
    public int LinqZip_IEnumerable()
    {
        int count = 0;
        foreach (var (value, timestamp) in _floatList.Zip(_uintList))
        {
            count++;
            // Simulate minimal work
            if (value > 0 && timestamp > 0) count += 0;
        }
        return count;
    }

    /// <summary>
    /// Custom ZipValues with IEnumerable (uses ref struct enumerator)
    /// </summary>
    [Benchmark]
    public int ZipValues_IEnumerable()
    {
        int count = 0;
        foreach (var (value, timestamp) in _floatList.ZipValues(_uintList))
        {
            count++;
            // Simulate minimal work
            if (value > 0 && timestamp > 0) count += 0;
        }
        return count;
    }

    /// <summary>
    /// Custom ZipValues with SampleAccessor (zero-allocation)
    /// </summary>
    [Benchmark]
    public int ZipValues_SampleAccessor()
    {
        int count = 0;
        foreach (var (value, timestamp) in _floatAccessor.ZipValues(_uintAccessor))
        {
            count++;
            // Simulate minimal work
            if (value > 0 && timestamp > 0) count += 0;
        }
        return count;
    }

    /// <summary>
    /// Custom ZipValues with SampleAccessor and IEnumerable (mixed)
    /// </summary>
    [Benchmark]
    public int ZipValues_SampleAccessor_IEnumerable()
    {
        int count = 0;
        foreach (var (value, timestamp) in _floatAccessor.ZipValues(_uintList))
        {
            count++;
            // Simulate minimal work
            if (value > 0 && timestamp > 0) count += 0;
        }
        return count;
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<ZipBenchmarks>();
    }
}
