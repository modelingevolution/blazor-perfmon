using System.Threading.Channels;
using System.Threading.Tasks.Dataflow;
using MessagePack;
using Microsoft.Extensions.Logging.Abstractions;
using ModelingEvolution.BlazorPerfMon.Server.Services;
using ModelingEvolution.BlazorPerfMon.Shared;

namespace ModelingEvolution.BlazorPerfMon.Tests;

/// <summary>
/// Tests for the metrics fan-out in MultiplexService.
///
/// Bug 007: the pipeline ended in a BroadcastBlock, which retains its most recent message and
/// offers it to any target linked later. A client that connected after the engine had stopped
/// therefore received a stale sample carrying old cumulative counters
/// (docs/bugs/bug-007-perfmon-network-first-sample-spike.md).
/// </summary>
public class MultiplexServiceTests
{
    /// <summary>How long to wait for a sample that is expected to arrive.</summary>
    private static readonly TimeSpan ArrivalTimeout = TimeSpan.FromSeconds(5);

    /// <summary>How long to wait to be convinced that nothing is going to arrive.</summary>
    private static readonly TimeSpan SilenceWindow = TimeSpan.FromMilliseconds(500);

    private static MultiplexService CreateService() => new(NullLogger<MultiplexService>.Instance);

    private static MetricTick Tick(uint timestampMs, ulong rxBytes) => new(
        CpuLoads: new[] { 10f },
        GpuLoads: new[] { 20f },
        Ram: new RamMetric { UsedBytes = 1024, TotalBytes = 4096 },
        NetworkMetrics: new[] { new NetworkMetric { Identifier = "eth0", RxBytes = rxBytes, TxBytes = 0 } },
        DiskMetrics: new[] { new DiskMetric { Identifier = "sda", ReadBytes = 0, WriteBytes = 0 } },
        DockerContainers: Array.Empty<DockerContainerMetric>(),
        TimestampMs: timestampMs,
        CollectionDurationMs: 12);

    /// <summary>
    /// Links a client target that records every sample it is sent.
    /// </summary>
    private static (ITargetBlock<byte[]> Target, ChannelReader<MetricSample> Received) LinkClient(MultiplexService service)
    {
        var channel = Channel.CreateUnbounded<MetricSample>();

        var target = service.CreateClientTarget(data =>
        {
            channel.Writer.TryWrite(MessagePackSerializer.Deserialize<MetricSample>(data));
            return Task.CompletedTask;
        });

        return (target, channel.Reader);
    }

    private static async Task<MetricSample> ReadOneAsync(ChannelReader<MetricSample> reader)
    {
        using var cts = new CancellationTokenSource(ArrivalTimeout);
        return await reader.ReadAsync(cts.Token);
    }

    private static async Task<bool> ReceivedAnythingAsync(ChannelReader<MetricSample> reader)
    {
        using var cts = new CancellationTokenSource(SilenceWindow);
        try
        {
            return await reader.WaitToReadAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    [Fact]
    public async Task PostMetrics_LinkedClient_ReceivesTheSample()
    {
        using var service = CreateService();
        var (target, received) = LinkClient(service);

        Assert.True(service.PostMetrics(Tick(1_000_000, 5_000)));

        var sample = await ReadOneAsync(received);

        Assert.Equal(1_000_000u, sample.CreatedAt);
        Assert.Equal(5_000ul, sample.NetworkMetrics![0].RxBytes);
        Assert.Equal(12u, sample.CollectionDurationMs);
        Assert.Equal(10f, sample.CpuAverage);
        Assert.Equal(20f, sample.GpuAverage);

        service.UnlinkClientTarget(target);
    }

    /// <summary>
    /// The bug scenario: a client connects, receives, disconnects; the engine keeps running for a
    /// moment; a new client connects. The new client must see nothing until a fresh tick is
    /// produced, never the sample retained from the previous session.
    /// </summary>
    [Fact]
    public async Task CreateClientTarget_AfterAnEarlierClientReceivedASample_ReceivesNoReplay()
    {
        using var service = CreateService();

        var (firstTarget, firstReceived) = LinkClient(service);
        service.PostMetrics(Tick(1_000_000, 5_000));
        await ReadOneAsync(firstReceived);
        service.UnlinkClientTarget(firstTarget);

        // The view is reopened.
        var (secondTarget, secondReceived) = LinkClient(service);

        Assert.False(await ReceivedAnythingAsync(secondReceived),
            "a newly linked client must not receive the sample broadcast before it linked");

        // A fresh tick does reach it.
        service.PostMetrics(Tick(1_060_000, 605_000_000));
        var sample = await ReadOneAsync(secondReceived);

        Assert.Equal(1_060_000u, sample.CreatedAt);
        Assert.Equal(605_000_000ul, sample.NetworkMetrics![0].RxBytes);

        service.UnlinkClientTarget(secondTarget);
    }

    /// <summary>
    /// Control test: a BroadcastBlock, the pre-fix fan-out, does replay its retained message to a
    /// target linked afterwards. This is the mechanism the test above proves is gone.
    /// </summary>
    [Fact]
    public async Task LegacyBroadcastBlock_ReplaysRetainedMessageToLateTarget()
    {
        var broadcast = new BroadcastBlock<byte[]>(bytes => bytes);
        var channel = Channel.CreateUnbounded<byte[]>();

        broadcast.Post(new byte[] { 1, 2, 3 });

        // Give the block a moment to take ownership of the message before anything links.
        await Task.Delay(50);

        var lateTarget = new ActionBlock<byte[]>(data =>
        {
            channel.Writer.TryWrite(data);
        });
        broadcast.LinkTo(lateTarget, new DataflowLinkOptions { PropagateCompletion = false });

        using var cts = new CancellationTokenSource(ArrivalTimeout);
        var replayed = await channel.Reader.ReadAsync(cts.Token);

        Assert.Equal(new byte[] { 1, 2, 3 }, replayed);
    }

    [Fact]
    public async Task PostMetrics_TwoLinkedClients_BothReceiveTheSample()
    {
        using var service = CreateService();

        var (firstTarget, firstReceived) = LinkClient(service);
        var (secondTarget, secondReceived) = LinkClient(service);

        service.PostMetrics(Tick(2_000_000, 7_000));

        Assert.Equal(2_000_000u, (await ReadOneAsync(firstReceived)).CreatedAt);
        Assert.Equal(2_000_000u, (await ReadOneAsync(secondReceived)).CreatedAt);

        service.UnlinkClientTarget(firstTarget);
        service.UnlinkClientTarget(secondTarget);
    }

    [Fact]
    public async Task UnlinkClientTarget_StopsDeliveryToThatClient()
    {
        using var service = CreateService();

        var (firstTarget, firstReceived) = LinkClient(service);
        var (secondTarget, secondReceived) = LinkClient(service);

        service.UnlinkClientTarget(firstTarget);
        service.PostMetrics(Tick(3_000_000, 9_000));

        Assert.Equal(3_000_000u, (await ReadOneAsync(secondReceived)).CreatedAt);
        Assert.False(await ReceivedAnythingAsync(firstReceived));

        service.UnlinkClientTarget(secondTarget);
    }

    [Fact]
    public void ClientEvents_FireOnFirstConnectAndLastDisconnect()
    {
        using var service = CreateService();

        int firstConnected = 0;
        int lastDisconnected = 0;
        service.FirstClientConnected += () => firstConnected++;
        service.LastClientDisconnected += () => lastDisconnected++;

        var first = service.CreateClientTarget(static _ => Task.CompletedTask);
        Assert.Equal(1, firstConnected);
        Assert.Equal(0, lastDisconnected);

        var second = service.CreateClientTarget(static _ => Task.CompletedTask);
        Assert.Equal(1, firstConnected);

        service.UnlinkClientTarget(first);
        Assert.Equal(0, lastDisconnected);

        service.UnlinkClientTarget(second);
        Assert.Equal(1, lastDisconnected);

        // Reopening the view starts the engine again.
        var third = service.CreateClientTarget(static _ => Task.CompletedTask);
        Assert.Equal(2, firstConnected);
        service.UnlinkClientTarget(third);
        Assert.Equal(2, lastDisconnected);
    }

    /// <summary>
    /// One post per tick means a tick either goes through whole or not at all - the pre-fix
    /// pipeline posted to four buffers and combined the results with '&amp;', so a single refused
    /// post left the JoinBlocks pairing metrics from different cycles from then on.
    /// </summary>
    [Fact]
    public async Task PostMetrics_ConsecutiveTicks_ArriveWholeAndInOrder()
    {
        using var service = CreateService();
        var (target, received) = LinkClient(service);

        for (uint i = 0; i < 5; i++)
        {
            service.PostMetrics(Tick(1_000_000 + i * 500, 5_000 + i * 1_000));

            var sample = await ReadOneAsync(received);
            Assert.Equal(1_000_000 + i * 500, sample.CreatedAt);
            Assert.Equal(5_000ul + i * 1_000, sample.NetworkMetrics![0].RxBytes);
        }

        service.UnlinkClientTarget(target);
    }
}
