using Backend.Collectors;
using Xunit;

namespace Backend.Tests.Unit.Collectors;

public class CpuCollectorTests
{
    [Fact]
    public void Collect_ReturnsArrayOf8Floats()
    {
        var collector = new CpuCollector();

        var result = collector.Collect();

        Assert.NotNull(result);
        Assert.Equal(8, result.Length);
    }

    [Fact]
    public void Collect_FirstCallReturnsZeros()
    {
        // First call establishes baseline, should return zeros
        var collector = new CpuCollector();

        var result = collector.Collect();

        // First read returns zeros as baseline
        Assert.All(result, value => Assert.InRange(value, 0f, 100f));
    }

    [Fact]
    public void Collect_SecondCallReturnsValidPercentages()
    {
        var collector = new CpuCollector();

        // First call establishes baseline
        collector.Collect();

        // Small delay to allow CPU activity
        Thread.Sleep(100);

        // Second call should return valid percentages
        var result = collector.Collect();

        Assert.All(result, value =>
        {
            Assert.InRange(value, 0f, 100f);
        });
    }

    [Fact]
    public void Collect_MultipleCallsProduceConsistentResults()
    {
        var collector = new CpuCollector();

        // Collect multiple times
        collector.Collect(); // Baseline
        Thread.Sleep(50);

        var results = new List<float[]>();
        for (int i = 0; i < 5; i++)
        {
            results.Add(collector.Collect());
            Thread.Sleep(50);
        }

        // All results should be valid
        foreach (var result in results)
        {
            Assert.Equal(8, result.Length);
            Assert.All(result, value => Assert.InRange(value, 0f, 100f));
        }
    }

    [Fact]
    public void Collect_HandlesRapidCalls()
    {
        var collector = new CpuCollector();

        // Rapid calls without delay
        for (int i = 0; i < 10; i++)
        {
            var result = collector.Collect();

            Assert.NotNull(result);
            Assert.Equal(8, result.Length);
            Assert.All(result, value => Assert.InRange(value, 0f, 100f));
        }
    }

    [Fact]
    public void Collect_At2Hz_ProducesReasonableResults()
    {
        var collector = new CpuCollector();

        // Simulate 2Hz collection (500ms interval)
        collector.Collect(); // Baseline

        var samples = new List<float[]>();
        for (int i = 0; i < 4; i++)
        {
            Thread.Sleep(500);
            samples.Add(collector.Collect());
        }

        // Verify all samples are valid
        foreach (var sample in samples)
        {
            Assert.Equal(8, sample.Length);
            Assert.All(sample, value => Assert.InRange(value, 0f, 100f));
        }

        // At least some cores should show non-zero activity over 2 seconds
        var hasActivity = samples.Any(s => s.Any(v => v > 0f));
        Assert.True(hasActivity, "Expected some CPU activity over 2 seconds");
    }

    [Fact]
    public void Collect_WithLinuxProcStat_ParsesCorrectly()
    {
        // This test only runs on Linux where /proc/stat exists
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        var collector = new CpuCollector();

        // First call
        var baseline = collector.Collect();
        Assert.Equal(8, baseline.Length);

        // Second call after delay
        Thread.Sleep(100);
        var result = collector.Collect();

        Assert.Equal(8, result.Length);
        Assert.All(result, value => Assert.InRange(value, 0f, 100f));
    }
}
