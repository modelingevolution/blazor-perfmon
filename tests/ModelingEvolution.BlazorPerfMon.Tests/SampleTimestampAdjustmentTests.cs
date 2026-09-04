using ModelingEvolution.BlazorPerfMon.Client.Services;

namespace ModelingEvolution.BlazorPerfMon.Tests;

/// <summary>
/// Tests for WebSocketClient.AdjustSampleTimestamp.
///
/// The server truncates epoch milliseconds to 32 bits. The pre-fix code compared that value
/// against full 64-bit browser milliseconds, so the difference was always ~1.7e12 and every
/// sample had its timestamp replaced by browser arrival time
/// (docs/bugs/bug-007-perfmon-network-first-sample-spike.md).
/// </summary>
public class SampleTimestampAdjustmentTests
{
    /// <summary>2026-09-04T00:00:00Z in epoch milliseconds - a realistic present-day value.</summary>
    private const long BrowserUnixTimeMs = 1_788_480_000_000L;

    private static uint ServerTimestamp(long unixTimeMs) => unchecked((uint)unixTimeMs);

    [Theory]
    [InlineData(0)]     // server and browser clocks agree exactly
    [InlineData(1)]
    [InlineData(120)]   // typical transport delay
    [InlineData(499)]   // last accepted value
    public void AdjustSampleTimestamp_WithinAcceptedSkew_ReturnsServerTimestampUnchanged(int transportDelayMs)
    {
        uint serverTimestampMs = ServerTimestamp(BrowserUnixTimeMs - transportDelayMs);

        uint adjusted = WebSocketClient.AdjustSampleTimestamp(serverTimestampMs, BrowserUnixTimeMs);

        Assert.Equal(serverTimestampMs, adjusted);
    }

    [Theory]
    [InlineData(500)]        // first rejected value
    [InlineData(5_000)]
    [InlineData(3_600_000)]  // server clock an hour behind
    public void AdjustSampleTimestamp_ServerClockBehind_ReturnsBrowserTimestamp(int skewMs)
    {
        uint serverTimestampMs = ServerTimestamp(BrowserUnixTimeMs - skewMs);

        uint adjusted = WebSocketClient.AdjustSampleTimestamp(serverTimestampMs, BrowserUnixTimeMs);

        Assert.Equal(ServerTimestamp(BrowserUnixTimeMs), adjusted);
    }

    [Fact]
    public void AdjustSampleTimestamp_ServerClockAhead_ReturnsBrowserTimestamp()
    {
        uint serverTimestampMs = ServerTimestamp(BrowserUnixTimeMs + 10_000);

        uint adjusted = WebSocketClient.AdjustSampleTimestamp(serverTimestampMs, BrowserUnixTimeMs);

        Assert.Equal(ServerTimestamp(BrowserUnixTimeMs), adjusted);
    }

    /// <summary>
    /// A stream of samples half a second apart must keep its spacing, because the rate
    /// calculation divides byte deltas by exactly that spacing.
    /// </summary>
    [Fact]
    public void AdjustSampleTimestamp_StreamAtNormalCadence_PreservesSpacing()
    {
        const int intervalMs = 500;
        const int transportDelayMs = 30;

        uint previous = 0;
        for (int i = 0; i < 10; i++)
        {
            long collectedAtMs = BrowserUnixTimeMs + i * intervalMs;
            uint adjusted = WebSocketClient.AdjustSampleTimestamp(
                ServerTimestamp(collectedAtMs),
                collectedAtMs + transportDelayMs);

            Assert.Equal(ServerTimestamp(collectedAtMs), adjusted);

            if (i > 0)
                Assert.Equal((uint)intervalMs, adjusted - previous);

            previous = adjusted;
        }
    }

    /// <summary>
    /// Two samples collected a minute apart, each delivered promptly, must still be a minute
    /// apart after adjustment. The chart's discontinuity guard can only see the gap if the
    /// adjustment preserves it.
    /// </summary>
    [Fact]
    public void AdjustSampleTimestamp_GapBetweenSamples_IsPreserved()
    {
        const int transportDelayMs = 40;
        long earlierCollectedAtMs = BrowserUnixTimeMs - 60_000;

        uint earlier = WebSocketClient.AdjustSampleTimestamp(
            ServerTimestamp(earlierCollectedAtMs), earlierCollectedAtMs + transportDelayMs);
        uint later = WebSocketClient.AdjustSampleTimestamp(
            ServerTimestamp(BrowserUnixTimeMs), BrowserUnixTimeMs + transportDelayMs);

        Assert.Equal(60_000u, later - earlier);
    }

    /// <summary>
    /// Control test: the pre-fix comparison, reproduced verbatim, rewrites every timestamp to
    /// browser arrival time even when the clocks agree perfectly.
    /// </summary>
    [Fact]
    public void LegacyAdjustSampleTimestamp_RewritesEveryTimestamp()
    {
        uint serverTimestampMs = ServerTimestamp(BrowserUnixTimeMs);

        // The pre-fix code never accepted a server timestamp as-is, not even with clocks in sync.
        Assert.False(LegacyAcceptsAsIs(serverTimestampMs, BrowserUnixTimeMs));

        // The fixed code accepts it.
        Assert.Equal(serverTimestampMs, WebSocketClient.AdjustSampleTimestamp(serverTimestampMs, BrowserUnixTimeMs));
    }

    /// <summary>
    /// The pre-fix acceptance test from WebSocketClient.AdjustSampleTimestamp, kept as a control.
    /// </summary>
    private static bool LegacyAcceptsAsIs(uint serverTimestampMs, long browserUnixTimeMs)
    {
        long differenceMs = browserUnixTimeMs - serverTimestampMs;
        return differenceMs >= 0 && differenceMs < 500;
    }
}
