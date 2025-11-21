using System.Net.WebSockets;
using MessagePack;
using ModelingEvolution.BlazorPerfMon.Shared;

namespace ModelingEvolution.BlazorPerfMon.Client.Services;

/// <summary>
/// WebSocket client with automatic reconnection.
/// Connects to backend metrics stream and deserializes MessagePack snapshots.
/// </summary>
public sealed class WebSocketClient : IAsyncDisposable
{
    private readonly string _wsUrl;
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;
    private bool _isDisposed;

    /// <summary>
    /// Event fired when configuration snapshot is received (first message).
    /// </summary>
    public event Action<PerformanceConfigurationSnapshot>? OnConfigurationReceived;

    /// <summary>
    /// Event fired when a new metrics sample is received.
    /// </summary>
    public event Action<MetricSample>? OnMetricsReceived;

    /// <summary>
    /// Event fired when connection state changes.
    /// </summary>
    public event Action<bool>? OnConnectionStateChanged;

    public WebSocketClient(string wsUrl)
    {
        _wsUrl = wsUrl;
    }

    /// <summary>
    /// Gets whether the WebSocket is currently connected.
    /// </summary>
    public bool IsConnected => _webSocket?.State == WebSocketState.Open;

    /// <summary>
    /// Start the WebSocket connection with automatic reconnection.
    /// </summary>
    public async Task ConnectAsync()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(WebSocketClient));

        _cts = new CancellationTokenSource();
        _receiveTask = ReceiveLoopAsync(_cts.Token);
        await Task.CompletedTask;
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Create new WebSocket connection
                _webSocket?.Dispose();
                _webSocket = new ClientWebSocket();

                Console.WriteLine("Connecting to WebSocket...");
                await _webSocket.ConnectAsync(new Uri(_wsUrl), cancellationToken);
                Console.WriteLine("WebSocket connected");

                OnConnectionStateChanged?.Invoke(true);

                // Receive loop
                var buffer = new byte[4096];
                using var ms = new MemoryStream();
                bool isFirstMessage = true;

                while (_webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Closing",
                            cancellationToken);
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        ms.Write(buffer, 0, result.Count);

                        if (result.EndOfMessage)
                        {
                            try
                            {
                                ms.Position = 0;

                                if (isFirstMessage)
                                {
                                    // First message is configuration snapshot
                                    var config = MessagePackSerializer.Deserialize<PerformanceConfigurationSnapshot>(ms);
                                    Console.WriteLine("Received configuration snapshot");
                                    OnConfigurationReceived?.Invoke(config);
                                    isFirstMessage = false;
                                }
                                else
                                {
                                    // Subsequent messages are metrics samples
                                    var sample = MessagePackSerializer.Deserialize<MetricSample>(ms);
                                    OnMetricsReceived?.Invoke(sample);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error deserializing metrics: {ex.Message}");
                            }
                            finally
                            {
                                ms.SetLength(0);
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket error: {ex.Message}");
            }

            OnConnectionStateChanged?.Invoke(false);

            // Auto-reconnect after 5 seconds
            if (!cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Reconnecting in 5 seconds...");
                await Task.Delay(5000, cancellationToken);
            }
        }

        Console.WriteLine("WebSocket receive loop exited");
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        _cts?.Cancel();
        _cts?.Dispose();

        if (_webSocket != null)
        {
            if (_webSocket.State == WebSocketState.Open)
            {
                try
                {
                    await _webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Client disposing",
                        CancellationToken.None);
                }
                catch
                {
                    // Ignore errors during disposal
                }
            }

            _webSocket.Dispose();
        }

        if (_receiveTask != null)
        {
            try
            {
                await _receiveTask;
            }
            catch
            {
                // Ignore errors during disposal
            }
        }
    }
}
