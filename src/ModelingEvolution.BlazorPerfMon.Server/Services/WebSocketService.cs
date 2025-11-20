using System.Buffers;
using System.Net.WebSockets;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;

namespace Backend.Services;

/// <summary>
/// Handles WebSocket connections and integrates with MultiplexService.
/// Each client gets a dedicated ActionBlock for sending metrics.
/// </summary>
public sealed class WebSocketService
{
    private readonly MultiplexService _multiplexService;
    private readonly ILogger<WebSocketService> _logger;

    public WebSocketService(MultiplexService multiplexService, ILogger<WebSocketService> logger)
    {
        _multiplexService = multiplexService;
        _logger = logger;
    }

    /// <summary>
    /// Handle a WebSocket connection. Creates an ActionBlock for this client
    /// and keeps the connection alive until it closes.
    /// </summary>
    public async Task HandleWebSocketAsync(WebSocket webSocket, CancellationToken cancellationToken)
    {
        ITargetBlock<byte[]>? clientTarget = null;

        try
        {
            _logger.LogInformation("WebSocket client connected");

            // Create an ActionBlock for this client
            clientTarget = _multiplexService.CreateClientTarget(async data =>
            {
                if (webSocket.State == WebSocketState.Open)
                {
                    try
                    {
                        // Use ArrayPool to avoid allocations
                        await webSocket.SendAsync(
                            new ReadOnlyMemory<byte>(data),
                            WebSocketMessageType.Binary,
                            endOfMessage: true,
                            cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending WebSocket message");
                    }
                }
            });

            // Keep connection alive and listen for close messages
            var buffer = ArrayPool<byte>.Shared.Rent(1024);
            try
            {
                while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var result = await webSocket.ReceiveAsync(
                        new Memory<byte>(buffer),
                        cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Closing",
                            cancellationToken);
                        break;
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebSocket error");
        }
        finally
        {
            if (clientTarget != null)
            {
                _multiplexService.UnlinkClientTarget(clientTarget);
            }

            if (webSocket.State == WebSocketState.Open)
            {
                try
                {
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.InternalServerError,
                        "Server error",
                        CancellationToken.None);
                }
                catch
                {
                    // Ignore errors during cleanup
                }
            }

            _logger.LogInformation("WebSocket client disconnected");
        }
    }
}
