using MessagePack;
using ProtoBuf;
using ProtoBuf.Meta;
using ModelingEvolution.BlazorPerfMon.Shared;
using Xunit.Abstractions;
using System.Diagnostics;

namespace ModelingEvolution.BlazorPerfMon.Tests;

/// <summary>
/// Tests to compare serialization sizes between MessagePack and protobuf-net.
/// </summary>
public class SerializationComparisonTests
{
    private static bool _protobufConfigured = false;
    private static readonly object _configLock = new object();
    private readonly ITestOutputHelper _output;

    public SerializationComparisonTests(ITestOutputHelper output)
    {
        _output = output;

        // Configure protobuf-net runtime type model for MetricSample and related types
        // Only configure once to avoid "frozen model" errors
        lock (_configLock)
        {
            if (!_protobufConfigured)
            {
                ConfigureProtobufModel();
                _protobufConfigured = true;
            }
        }
    }

    private static void ConfigureProtobufModel()
    {
        var model = RuntimeTypeModel.Default;

        // Configure MetricSample
        model.Add(typeof(MetricSample), false)
            .Add(1, nameof(MetricSample.CreatedAt))
            .Add(2, nameof(MetricSample.CollectionDurationMs))
            .Add(3, nameof(MetricSample.CpuLoads))
            .Add(4, nameof(MetricSample.CpuAverage))
            .Add(5, nameof(MetricSample.GpuLoads))
            .Add(6, nameof(MetricSample.GpuAverage))
            .Add(7, nameof(MetricSample.Ram))
            .Add(8, nameof(MetricSample.NetworkMetrics))
            .Add(9, nameof(MetricSample.DiskMetrics))
            .Add(10, nameof(MetricSample.DockerContainers))
            .Add(11, nameof(MetricSample.Temperatures));

        // Configure RamMetric
        model.Add(typeof(RamMetric), false)
            .Add(1, nameof(RamMetric.UsedBytes))
            .Add(2, nameof(RamMetric.TotalBytes));

        // Configure NetworkMetric
        model.Add(typeof(NetworkMetric), false)
            .Add(1, nameof(NetworkMetric.Identifier))
            .Add(2, nameof(NetworkMetric.RxBytes))
            .Add(3, nameof(NetworkMetric.TxBytes));

        // Configure DiskMetric
        model.Add(typeof(DiskMetric), false)
            .Add(1, nameof(DiskMetric.Identifier))
            .Add(2, nameof(DiskMetric.ReadBytes))
            .Add(3, nameof(DiskMetric.WriteBytes))
            .Add(4, nameof(DiskMetric.ReadIops))
            .Add(5, nameof(DiskMetric.WriteIops));

        // Configure DockerContainerMetric
        model.Add(typeof(DockerContainerMetric), false)
            .Add(1, nameof(DockerContainerMetric.ContainerId))
            .Add(2, nameof(DockerContainerMetric.Name))
            .Add(3, nameof(DockerContainerMetric.CpuPercent))
            .Add(4, nameof(DockerContainerMetric.MemoryUsageBytes))
            .Add(5, nameof(DockerContainerMetric.MemoryLimitBytes));

        // Configure TemperatureMetric
        model.Add(typeof(TemperatureMetric), false)
            .Add(1, nameof(TemperatureMetric.Sensor))
            .Add(2, nameof(TemperatureMetric.TempCelsius));
    }
    /// <summary>
    /// Creates a realistic MetricSample with typical production data.
    /// </summary>
    private MetricSample CreateRealisticSample(uint? timestampOverride = null)
    {
        return new MetricSample
        {
            CreatedAt = timestampOverride ?? 1234567890,
            CollectionDurationMs = 15,

            // CPU metrics - 16 cores
            CpuLoads = new[] { 45.2f, 32.1f, 67.8f, 23.4f, 89.1f, 12.3f, 56.7f, 34.5f,
                              78.9f, 21.4f, 43.2f, 65.8f, 19.7f, 87.3f, 41.6f, 29.8f },
            CpuAverage = 48.5f,

            // GPU metrics - 2 GPUs
            GpuLoads = new[] { 75.3f, 0.0f },
            GpuAverage = 37.65f,

            // RAM
            Ram = new RamMetric
            {
                UsedBytes = 8_589_934_592,  // 8 GB
                TotalBytes = 16_106_127_360  // 15 GB
            },

            // Network - 2 interfaces
            NetworkMetrics = new[]
            {
                new NetworkMetric
                {
                    Identifier = "eth0",
                    RxBytes = 123456789,
                    TxBytes = 987654321
                },
                new NetworkMetric
                {
                    Identifier = "wlan0",
                    RxBytes = 0,
                    TxBytes = 0
                }
            },

            // Disk - 2 devices
            DiskMetrics = new[]
            {
                new DiskMetric
                {
                    Identifier = "sda",
                    ReadBytes = 456789123,
                    WriteBytes = 321987654,
                    ReadIops = 1234,
                    WriteIops = 5678
                },
                new DiskMetric
                {
                    Identifier = "nvme0n1",
                    ReadBytes = 789123456,
                    WriteBytes = 654321789,
                    ReadIops = 9012,
                    WriteIops = 3456
                }
            },

            // Docker containers - 3 containers
            DockerContainers = new[]
            {
                new DockerContainerMetric
                {
                    ContainerId = "abc123def456",
                    Name = "web-server",
                    CpuPercent = 12.5f,
                    MemoryUsageBytes = 536870912,  // 512 MB
                    MemoryLimitBytes = 1073741824   // 1 GB
                },
                new DockerContainerMetric
                {
                    ContainerId = "def456ghi789",
                    Name = "database",
                    CpuPercent = 45.8f,
                    MemoryUsageBytes = 2147483648,  // 2 GB
                    MemoryLimitBytes = 4294967296   // 4 GB
                },
                new DockerContainerMetric
                {
                    ContainerId = "ghi789jkl012",
                    Name = "cache",
                    CpuPercent = 8.3f,
                    MemoryUsageBytes = 268435456,   // 256 MB
                    MemoryLimitBytes = 536870912    // 512 MB
                }
            },

            // Temperature - 9 sensors (typical for Jetson)
            Temperatures = new[]
            {
                new TemperatureMetric { Sensor = "cpu", TempCelsius = 46.5f },
                new TemperatureMetric { Sensor = "gpu", TempCelsius = 43.2f },
                new TemperatureMetric { Sensor = "soc0", TempCelsius = 44.8f },
                new TemperatureMetric { Sensor = "soc1", TempCelsius = 45.1f },
                new TemperatureMetric { Sensor = "soc2", TempCelsius = 43.9f },
                new TemperatureMetric { Sensor = "cv0", TempCelsius = 43.6f },
                new TemperatureMetric { Sensor = "cv1", TempCelsius = 43.4f },
                new TemperatureMetric { Sensor = "cv2", TempCelsius = 43.8f },
                new TemperatureMetric { Sensor = "tj", TempCelsius = 47.0f }
            }
        };
    }

    [Fact]
    public void CompareSerializationSizes_MessagePackVsProtobuf()
    {
        // Arrange
        var sample = CreateRealisticSample();
        var sw = Stopwatch.StartNew();

        // Serialize with MessagePack
        sw.Restart();
        byte[] messagePackBytes = MessagePackSerializer.Serialize(sample);
        var messagePackSerializeMs = sw.Elapsed.TotalMilliseconds;

        // Serialize with protobuf-net
        byte[] protobufBytes;
        sw.Restart();
        using (var ms = new MemoryStream())
        {
            Serializer.Serialize(ms, sample);
            protobufBytes = ms.ToArray();
        }
        var protobufSerializeMs = sw.Elapsed.TotalMilliseconds;

        // Output results
        int messagePackSize = messagePackBytes.Length;
        int protobufSize = protobufBytes.Length;
        int difference = messagePackSize - protobufSize;
        double percentDifference = ((double)difference / messagePackSize) * 100;

        // Deserialize with MessagePack
        sw.Restart();
        var messagePackDeserialized = MessagePackSerializer.Deserialize<MetricSample>(messagePackBytes);
        var messagePackDeserializeMs = sw.Elapsed.TotalMilliseconds;

        // Deserialize with protobuf-net
        MetricSample protobufDeserialized;
        sw.Restart();
        using (var ms = new MemoryStream(protobufBytes))
        {
            protobufDeserialized = Serializer.Deserialize<MetricSample>(ms);
        }
        var protobufDeserializeMs = sw.Elapsed.TotalMilliseconds;

        // Log results for visibility
        string changeType = difference > 0 ? "reduction" : "increase";
        _output.WriteLine($"MessagePack size: {messagePackSize} bytes");
        _output.WriteLine($"Protobuf-net size: {protobufSize} bytes");
        _output.WriteLine($"Difference: {difference} bytes ({percentDifference:F2}% {changeType})");
        _output.WriteLine($"Protobuf-net is {(double)messagePackSize / protobufSize:F2}x the size of MessagePack");
        _output.WriteLine("");
        _output.WriteLine($"MessagePack serialize: {messagePackSerializeMs:F3} ms");
        _output.WriteLine($"MessagePack deserialize: {messagePackDeserializeMs:F3} ms");
        _output.WriteLine($"Protobuf-net serialize: {protobufSerializeMs:F3} ms");
        _output.WriteLine($"Protobuf-net deserialize: {protobufDeserializeMs:F3} ms");

        // Basic sanity checks
        Assert.Equal(sample.CreatedAt, messagePackDeserialized.CreatedAt);
        Assert.Equal(sample.CreatedAt, protobufDeserialized.CreatedAt);
        Assert.Equal(sample.CpuLoads?.Length, messagePackDeserialized.CpuLoads?.Length);
        Assert.Equal(sample.CpuLoads?.Length, protobufDeserialized.CpuLoads?.Length);
    }

    [Fact]
    public void CompareSerializationSizes_Multiplesamples()
    {
        // Arrange - simulate 120 samples (1 minute at 500ms intervals)
        const int sampleCount = 120;
        var samples = new MetricSample[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            samples[i] = CreateRealisticSample((uint)(1234567890 + i * 500));
        }

        var sw = Stopwatch.StartNew();

        // Serialize arrays with MessagePack
        sw.Restart();
        byte[] messagePackBytes = MessagePackSerializer.Serialize(samples);
        var messagePackSerializeMs = sw.Elapsed.TotalMilliseconds;

        // Serialize arrays with protobuf-net
        byte[] protobufBytes;
        sw.Restart();
        using (var ms = new MemoryStream())
        {
            Serializer.Serialize(ms, samples);
            protobufBytes = ms.ToArray();
        }
        var protobufSerializeMs = sw.Elapsed.TotalMilliseconds;

        // Output results
        int messagePackSize = messagePackBytes.Length;
        int protobufSize = protobufBytes.Length;
        int difference = messagePackSize - protobufSize;
        double percentDifference = ((double)difference / messagePackSize) * 100;

        // Calculate per-sample averages
        double messagePackAvg = (double)messagePackSize / sampleCount;
        double protobufAvg = (double)protobufSize / sampleCount;

        // Deserialize arrays with MessagePack
        sw.Restart();
        var messagePackDeserialized = MessagePackSerializer.Deserialize<MetricSample[]>(messagePackBytes);
        var messagePackDeserializeMs = sw.Elapsed.TotalMilliseconds;

        // Deserialize arrays with protobuf-net
        MetricSample[] protobufDeserialized;
        sw.Restart();
        using (var ms = new MemoryStream(protobufBytes))
        {
            protobufDeserialized = Serializer.Deserialize<MetricSample[]>(ms);
        }
        var protobufDeserializeMs = sw.Elapsed.TotalMilliseconds;

        string changeType2 = difference > 0 ? "reduction" : "increase";
        _output.WriteLine($"\n=== {sampleCount} Samples Comparison ===");
        _output.WriteLine($"MessagePack total: {messagePackSize:N0} bytes ({messagePackAvg:F1} bytes/sample)");
        _output.WriteLine($"Protobuf-net total: {protobufSize:N0} bytes ({protobufAvg:F1} bytes/sample)");
        _output.WriteLine($"Difference: {difference:N0} bytes ({percentDifference:F2}% {changeType2})");
        _output.WriteLine($"Bandwidth savings at 2Hz: {(difference * 2):N0} bytes/sec = {(difference * 2 / 1024.0):F2} KB/sec");
        _output.WriteLine("");
        _output.WriteLine($"MessagePack serialize: {messagePackSerializeMs:F3} ms");
        _output.WriteLine($"MessagePack deserialize: {messagePackDeserializeMs:F3} ms");
        _output.WriteLine($"Protobuf-net serialize: {protobufSerializeMs:F3} ms");
        _output.WriteLine($"Protobuf-net deserialize: {protobufDeserializeMs:F3} ms");

        Assert.Equal(sampleCount, messagePackDeserialized.Length);
        Assert.Equal(sampleCount, protobufDeserialized.Length);
    }
}
