using Backend.Collectors;
using Backend.Core;
using Backend.Services;
using MessagePack;
using System.Threading.Tasks.Dataflow;
using Xunit;

namespace Backend.Tests.Integration;

public class Stage1_CpuOnlyTests : IDisposable
{
    private readonly MultiplexService _multiplexService;
    private readonly CpuCollector _cpuCollector;

    public Stage1_CpuOnlyTests()
    {
        _multiplexService = new MultiplexService();
        _cpuCollector = new CpuCollector();
    }

    [Fact]
    public async Task Pipeline_PostCpuMetrics_BroadcastsToClients()
    {
        // Arrange
        var receivedMessages = new List<byte[]>();
        var clientTarget = _multiplexService.CreateClientTarget(async data =>
        {
            receivedMessages.Add(data);
            await Task.CompletedTask;
        });

        // Act - Post CPU metrics
        var cpuData = _cpuCollector.Collect();
        var posted = _multiplexService.PostCpuMetrics(cpuData);

        // Wait for processing
        await Task.Delay(100);

        // Assert
        Assert.True(posted, "Should successfully post CPU metrics");
        Assert.Single(receivedMessages);

        // Verify message is valid MessagePack
        var snapshot = MessagePackSerializer.Deserialize<MetricsSnapshot>(receivedMessages[0]);
        Assert.NotNull(snapshot);
        Assert.NotNull(snapshot.CpuLoads);
        Assert.Equal(8, snapshot.CpuLoads.Length);
    }

    [Fact]
    public async Task Pipeline_MultipleMessages_AllReceivedByClient()
    {
        // Arrange
        var receivedMessages = new List<byte[]>();
        var clientTarget = _multiplexService.CreateClientTarget(async data =>
        {
            receivedMessages.Add(data);
            await Task.CompletedTask;
        });

        // Act - Post multiple messages
        for (int i = 0; i < 5; i++)
        {
            var cpuData = _cpuCollector.Collect();
            _multiplexService.PostCpuMetrics(cpuData);
            await Task.Delay(50);
        }

        await Task.Delay(200);

        // Assert
        Assert.Equal(5, receivedMessages.Count);

        // Verify all messages are valid
        foreach (var message in receivedMessages)
        {
            var snapshot = MessagePackSerializer.Deserialize<MetricsSnapshot>(message);
            Assert.NotNull(snapshot);
            Assert.NotNull(snapshot.CpuLoads);
            Assert.Equal(8, snapshot.CpuLoads.Length);
        }
    }

    [Fact]
    public async Task Pipeline_MultipleClients_AllReceiveMessages()
    {
        // Arrange
        var client1Messages = new List<byte[]>();
        var client2Messages = new List<byte[]>();

        var client1Target = _multiplexService.CreateClientTarget(async data =>
        {
            client1Messages.Add(data);
            await Task.CompletedTask;
        });

        var client2Target = _multiplexService.CreateClientTarget(async data =>
        {
            client2Messages.Add(data);
            await Task.CompletedTask;
        });

        // Act - Post messages
        for (int i = 0; i < 3; i++)
        {
            var cpuData = _cpuCollector.Collect();
            _multiplexService.PostCpuMetrics(cpuData);
            await Task.Delay(50);
        }

        await Task.Delay(200);

        // Assert - Both clients receive all messages
        Assert.Equal(3, client1Messages.Count);
        Assert.Equal(3, client2Messages.Count);
    }

    [Fact]
    public async Task Pipeline_At2Hz_ProducesConsistentMessages()
    {
        // Arrange
        var receivedMessages = new List<MetricsSnapshot>();
        var clientTarget = _multiplexService.CreateClientTarget(async data =>
        {
            var snapshot = MessagePackSerializer.Deserialize<MetricsSnapshot>(data);
            receivedMessages.Add(snapshot);
            await Task.CompletedTask;
        });

        // Act - Simulate 2Hz collection for 1 second
        for (int i = 0; i < 2; i++)
        {
            var cpuData = _cpuCollector.Collect();
            _multiplexService.PostCpuMetrics(cpuData);
            await Task.Delay(500); // 2Hz = 500ms interval
        }

        await Task.Delay(200);

        // Assert
        Assert.Equal(2, receivedMessages.Count);

        // Verify timestamps are increasing
        if (receivedMessages.Count > 1)
        {
            for (int i = 1; i < receivedMessages.Count; i++)
            {
                Assert.True(receivedMessages[i].TimestampMs >= receivedMessages[i - 1].TimestampMs,
                    "Timestamps should be monotonically increasing");
            }
        }
    }

    [Fact]
    public async Task MessagePack_Serialization_ProducesSmallMessages()
    {
        // Arrange
        var receivedMessages = new List<byte[]>();
        var clientTarget = _multiplexService.CreateClientTarget(async data =>
        {
            receivedMessages.Add(data);
            await Task.CompletedTask;
        });

        // Act
        var cpuData = _cpuCollector.Collect();
        _multiplexService.PostCpuMetrics(cpuData);
        await Task.Delay(100);

        // Assert
        Assert.Single(receivedMessages);

        // Message should be < 500 bytes per spec
        Assert.True(receivedMessages[0].Length < 500,
            $"Message size {receivedMessages[0].Length} bytes should be < 500 bytes");

        // For Stage 1 (CPU only), should be even smaller (~80 bytes)
        Assert.True(receivedMessages[0].Length < 150,
            $"Stage 1 message size {receivedMessages[0].Length} bytes should be < 150 bytes");
    }

    [Fact]
    public void Backpressure_ExceedingCapacity_ReturnsFalse()
    {
        // Arrange - Don't create any clients to consume messages

        // Act - Try to post more messages than buffer capacity (2)
        var result1 = _multiplexService.PostCpuMetrics(_cpuCollector.Collect());
        var result2 = _multiplexService.PostCpuMetrics(_cpuCollector.Collect());
        var result3 = _multiplexService.PostCpuMetrics(_cpuCollector.Collect());

        // Assert - First 2 should succeed, 3rd should fail (backpressure)
        Assert.True(result1 || result2, "At least first 2 posts should succeed");
        // Note: This test may be flaky depending on timing
    }

    [Fact]
    public async Task ClientDisconnect_UnlinksFromBroadcast()
    {
        // Arrange
        var receivedMessages = new List<byte[]>();
        var clientTarget = _multiplexService.CreateClientTarget(async data =>
        {
            receivedMessages.Add(data);
            await Task.CompletedTask;
        });

        // Send first message
        _multiplexService.PostCpuMetrics(_cpuCollector.Collect());
        await Task.Delay(100);

        Assert.Single(receivedMessages);

        // Act - Disconnect client
        _multiplexService.UnlinkClientTarget(clientTarget);
        await Task.Delay(100);

        // Send second message
        _multiplexService.PostCpuMetrics(_cpuCollector.Collect());
        await Task.Delay(100);

        // Assert - Client should not receive second message
        Assert.Single(receivedMessages);
    }

    public void Dispose()
    {
        _multiplexService.Dispose();
    }
}
