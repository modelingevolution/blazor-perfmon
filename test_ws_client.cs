using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;

var ws = new ClientWebSocket();
await ws.ConnectAsync(new Uri("ws://localhost:5062/ws"), CancellationToken.None);
Console.WriteLine("Connected to WebSocket!");

var buffer = new byte[4096];
int messageCount = 0;

while (ws.State == WebSocketState.Open && messageCount < 10)
{
    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

    if (result.MessageType == WebSocketMessageType.Binary)
    {
        var data = MessagePackSerializer.Deserialize<Dictionary<string, object>>(
            buffer.AsMemory(0, result.Count));

        var timestamp = data.ContainsKey("TimestampMs") ? data["TimestampMs"] : 0;
        var cpuLoads = data.ContainsKey("CpuLoads") ? (object[])data["CpuLoads"] : Array.Empty<object>();

        Console.WriteLine($"[{timestamp}] Received {result.Count} bytes, {cpuLoads.Length} CPU cores");

        for (int i = 0; i < Math.Min(cpuLoads.Length, 16); i++)
        {
            Console.WriteLine($"  CPU{i}: {cpuLoads[i]}%");
        }
        Console.WriteLine();

        messageCount++;
    }
}

await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
Console.WriteLine($"Test complete. Received {messageCount} messages.");
