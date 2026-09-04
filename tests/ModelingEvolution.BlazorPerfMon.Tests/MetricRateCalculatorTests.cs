using ModelingEvolution.BlazorPerfMon.Client.Collections;
using ModelingEvolution.BlazorPerfMon.Client.Extensions;
using ModelingEvolution.BlazorPerfMon.Client.Rendering;
using ModelingEvolution.BlazorPerfMon.Shared;

namespace ModelingEvolution.BlazorPerfMon.Tests;

/// <summary>
/// Tests for the rate calculation shared by NetworkChart and DiskChart, including the
/// discontinuity guard added for bug 007
/// (docs/bugs/bug-007-perfmon-network-first-sample-spike.md).
/// </summary>
public class MetricRateCalculatorTests
{
    private const string Interface = "eth0";
    private const float IntervalSec = 0.5f;
    private const uint IntervalMs = 500;

    private static MetricRateCalculator<NetworkMetric> CreateCalculator() =>
        new(Interface, IntervalSec, static m => m.Identifier);

    private static MetricSample Sample(uint timestampMs, ulong rxBytes) => new()
    {
        CreatedAt = timestampMs,
        NetworkMetrics = new[]
        {
            new NetworkMetric { Identifier = Interface, RxBytes = rxBytes, TxBytes = 0 }
        }
    };

    private static float Rate(MetricRateCalculator<NetworkMetric> calculator, in MetricSample current, in MetricSample previous) =>
        calculator.Calculate(current.NetworkMetrics, current.CreatedAt, previous.NetworkMetrics, previous.CreatedAt, static m => m.RxBytes);

    [Fact]
    public void Calculate_NormalCadence_ReturnsBytesPerSecond()
    {
        var calculator = CreateCalculator();
        var previous = Sample(1_000_000, 10_000);
        var current = Sample(1_000_000 + IntervalMs, 15_000);

        // 5000 bytes over 0.5 s
        Assert.Equal(10_000f, Rate(calculator, current, previous));
    }

    [Fact]
    public void Calculate_NoPreviousSample_ReturnsZero()
    {
        var calculator = CreateCalculator();

        Assert.Equal(0f, Rate(calculator, Sample(1_000_000, 10_000), default));
    }

    [Fact]
    public void Calculate_CounterWentBackwards_ReturnsZero()
    {
        var calculator = CreateCalculator();
        var previous = Sample(1_000_000, 15_000);
        var current = Sample(1_000_000 + IntervalMs, 10_000);

        Assert.Equal(0f, Rate(calculator, current, previous));
    }

    [Fact]
    public void Calculate_SameTimestamp_ReturnsZero()
    {
        var calculator = CreateCalculator();
        var previous = Sample(1_000_000, 10_000);
        var current = Sample(1_000_000, 99_000);

        Assert.Equal(0f, Rate(calculator, current, previous));
    }

    [Fact]
    public void Calculate_UnknownInterface_ReturnsZero()
    {
        var calculator = new MetricRateCalculator<NetworkMetric>("wlan0", IntervalSec, static m => m.Identifier);
        var previous = Sample(1_000_000, 10_000);
        var current = Sample(1_000_000 + IntervalMs, 15_000);

        Assert.Equal(0f, Rate(calculator, current, previous));
    }

    [Theory]
    // Gaps up to 4 collection intervals are real measurements.
    [InlineData(500u, true)]
    [InlineData(1000u, true)]
    [InlineData(2000u, true)]
    // Beyond 4 intervals the gap is a discontinuity and the rate is suppressed.
    [InlineData(2001u, false)]
    [InlineData(5000u, false)]
    [InlineData(60000u, false)]
    public void Calculate_GapBeyondFourIntervals_IsTreatedAsDiscontinuity(uint gapMs, bool expectRate)
    {
        var calculator = CreateCalculator();
        var previous = Sample(1_000_000, 10_000);
        var current = Sample(1_000_000 + gapMs, 10_000 + gapMs); // 1 byte per ms => 1000 B/s

        float rate = Rate(calculator, current, previous);

        if (expectRate)
            Assert.Equal(1000f, rate);
        else
            Assert.Equal(0f, rate);
    }

    /// <summary>
    /// Bug 007: the view is closed for a minute, the server keeps counting, and the first live
    /// sample arrives after the stale one. With honest server timestamps the gap is the whole
    /// outage, so the discontinuity guard suppresses the spike.
    /// </summary>
    [Fact]
    public void Calculate_StaleSampleFollowedByLiveSample_ReturnsZero()
    {
        var calculator = CreateCalculator();

        // Last sample seen before the view was closed.
        var stale = Sample(1_000_000, 1_000_000);

        // 60 s later the view is reopened; 600 MB crossed the interface meanwhile.
        var live = Sample(1_000_000 + 60_000, 1_000_000 + 600_000_000);

        Assert.Equal(0f, Rate(calculator, live, stale));

        // The sample after it is a normal measurement again.
        var next = Sample(live.CreatedAt + IntervalMs, live.NetworkMetrics![0].RxBytes + 5_000);
        Assert.Equal(10_000f, Rate(calculator, next, live));
    }

    /// <summary>
    /// Control test: the pre-fix code, reproduced verbatim, does produce the spike the bug reports.
    /// If this ever stops failing to guard, the test above is not proving anything.
    /// </summary>
    [Fact]
    public void LegacyCalculate_StaleSampleFollowedByLiveSample_ProducesSpike()
    {
        var stale = Sample(1_000_000, 1_000_000);
        var live = Sample(1_000_000 + 60_000, 1_000_000 + 600_000_000);

        float legacyRate = LegacyCalculateRate(live, stale);

        // 600 MB over 60 s = 10 MB/s, two orders of magnitude above any real rate on this link,
        // and it stays on screen for the whole client buffer.
        Assert.Equal(10_000_000f, legacyRate);
        Assert.Equal(0f, Rate(CreateCalculator(), live, stale));
    }

    /// <summary>
    /// The pre-fix NetworkChart.CalculateRate body, kept as a control for the test above.
    /// </summary>
    private static float LegacyCalculateRate(in MetricSample current, in MetricSample previous)
    {
        if (previous.CreatedAt == 0)
            return 0f;

        uint durationMs = current.CreatedAt - previous.CreatedAt;
        if (durationMs == 0)
            return 0f;

        var currentMetrics = current.NetworkMetrics;
        var previousMetrics = previous.NetworkMetrics;

        if (currentMetrics == null || previousMetrics == null)
            return 0f;

        int index = currentMetrics.IndexOf(n => n.Identifier == Interface);
        if (index == -1 || index >= previousMetrics.Length)
            return 0f;

        ulong currentValue = currentMetrics[index].RxBytes;
        ulong previousValue = previousMetrics[index].RxBytes;

        if (currentValue < previousValue)
            return 0f;

        return (currentValue - previousValue) / (durationMs / 1000f);
    }

    [Fact]
    public void Constructor_NonPositiveInterval_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new MetricRateCalculator<NetworkMetric>(Interface, 0f, static m => m.Identifier));
    }

    /// <summary>
    /// The charts read rates through SampleDeltaAccessor, so the guard has to hold when driven
    /// from a real buffer: the accessor pairs each sample with its predecessor.
    /// </summary>
    [Fact]
    public void SampleDeltaAccessor_BufferWithStaleThenLiveSamples_YieldsZeroForFirstLiveSample()
    {
        var calculator = CreateCalculator();
        var buffer = new ImmutableCircularBuffer<MetricSample>(8);

        var accessor = new SampleDeltaAccessor<float>(buffer,
            (in MetricSample current, in MetricSample previous) => Rate(calculator, current, previous));

        // One sample from before the view was closed, then three live ones a minute later.
        buffer = buffer.Add(Sample(1_000_000, 1_000_000));
        buffer = buffer.Add(Sample(1_061_000, 601_000_000));
        buffer = buffer.Add(Sample(1_061_500, 601_005_000));
        buffer = buffer.Add(Sample(1_062_000, 601_010_000));
        accessor.UpdateBuffer(buffer);

        var rates = accessor.ToArray();

        Assert.Equal(4, rates.Length);
        Assert.Equal(0f, rates[0]);     // no predecessor
        Assert.Equal(0f, rates[1]);     // first live sample after the gap - the bug
        Assert.Equal(10_000f, rates[2]);
        Assert.Equal(10_000f, rates[3]);
        Assert.Equal(10_000f, accessor.Last());
    }
}
