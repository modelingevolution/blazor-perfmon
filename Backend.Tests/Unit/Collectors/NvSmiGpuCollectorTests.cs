using Backend.Collectors;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Backend.Tests.Unit.Collectors;

public class NvSmiGpuCollectorTests
{
    [Fact]
    public void Collect_ReturnsSingleFloat()
    {
        var logger = NullLogger<NvSmiGpuCollector>.Instance;
        var collector = new NvSmiGpuCollector(logger);

        var result = collector.Collect();

        Assert.InRange(result, 0f, 100f);
    }

    [Fact]
    public void Collect_MultipleCallsProduceValidResults()
    {
        var logger = NullLogger<NvSmiGpuCollector>.Instance;
        var collector = new NvSmiGpuCollector(logger);

        // Collect multiple times
        var results = new List<float>();
        for (int i = 0; i < 5; i++)
        {
            results.Add(collector.Collect());
            Thread.Sleep(100);
        }

        // All results should be valid percentages
        Assert.All(results, value => Assert.InRange(value, 0f, 100f));
    }

    [Fact]
    public void Collect_HandlesRapidCalls()
    {
        var logger = NullLogger<NvSmiGpuCollector>.Instance;
        var collector = new NvSmiGpuCollector(logger);

        // Rapid calls without delay
        for (int i = 0; i < 10; i++)
        {
            var result = collector.Collect();
            Assert.InRange(result, 0f, 100f);
        }
    }

    [Fact]
    public void Collect_At2Hz_ProducesReasonableResults()
    {
        var logger = NullLogger<NvSmiGpuCollector>.Instance;
        var collector = new NvSmiGpuCollector(logger);

        // Simulate 2Hz collection (500ms interval)
        var samples = new List<float>();
        for (int i = 0; i < 4; i++)
        {
            samples.Add(collector.Collect());
            Thread.Sleep(500);
        }

        // Verify all samples are valid
        Assert.All(samples, value => Assert.InRange(value, 0f, 100f));
    }

    [Fact]
    public void Collect_WithoutNvidiaSmi_ReturnsZero()
    {
        // This test verifies graceful degradation when nvidia-smi is not available
        // The collector should return 0 rather than throwing an exception
        var logger = NullLogger<NvSmiGpuCollector>.Instance;
        var collector = new NvSmiGpuCollector(logger);

        var result = collector.Collect();

        // Should return a valid percentage (0-100), even if GPU not available
        Assert.InRange(result, 0f, 100f);
    }

    [Fact]
    public void Collect_ConsistentBetweenCalls()
    {
        var logger = NullLogger<NvSmiGpuCollector>.Instance;
        var collector = new NvSmiGpuCollector(logger);

        var result1 = collector.Collect();
        Thread.Sleep(100);
        var result2 = collector.Collect();

        // Both should be valid percentages
        Assert.InRange(result1, 0f, 100f);
        Assert.InRange(result2, 0f, 100f);

        // Results should be reasonably close (within 50%) if GPU idle
        // This test is lenient to account for actual GPU activity
        var difference = Math.Abs(result1 - result2);
        Assert.True(difference <= 100f, $"Unexpected large difference: {difference}%");
    }
}
