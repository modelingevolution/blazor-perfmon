using Backend.Collectors;
using Backend.Core;
using Backend.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Configure settings
builder.Services.Configure<MonitorSettings>(
    builder.Configuration.GetSection("MonitorSettings"));

// Register services as singletons (single instance for application lifetime)
builder.Services.AddSingleton<CpuCollector>();
builder.Services.AddSingleton<NetworkCollector>();
builder.Services.AddSingleton<DiskCollector>();
builder.Services.AddSingleton<MultiplexService>();
builder.Services.AddSingleton<WebSocketService>();

var app = builder.Build();

// Configure Blazor WebAssembly hosting
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();

// Enable WebSockets
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

// WebSocket endpoint for metrics streaming
app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var wsService = context.RequestServices.GetRequiredService<WebSocketService>();
        await wsService.HandleWebSocketAsync(webSocket, context.RequestAborted);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

// Fallback to index.html for Blazor client-side routing
app.MapFallbackToFile("index.html");

// Start metrics collection using PeriodicTimer
var cpuCollector = app.Services.GetRequiredService<CpuCollector>();
var networkCollector = app.Services.GetRequiredService<NetworkCollector>();
var diskCollector = app.Services.GetRequiredService<DiskCollector>();
var multiplexService = app.Services.GetRequiredService<MultiplexService>();
var settings = app.Services.GetRequiredService<IOptions<MonitorSettings>>().Value;
var logger = app.Services.GetRequiredService<ILogger<Program>>();

var metricsTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(settings.CollectionIntervalMs));
var cancellationTokenSource = new CancellationTokenSource();

// Background task for metrics collection
var stopwatch = new System.Diagnostics.Stopwatch();
_ = Task.Run(async () =>
{
    while (await metricsTimer.WaitForNextTickAsync(cancellationTokenSource.Token))
    {
        try
        {
            stopwatch.Restart();

            // Collect metrics in parallel
            var (cpuData, networkData, diskData) = await Task.WhenAll(
                Task.Run(cpuCollector.Collect, cancellationTokenSource.Token),
                Task.Run(networkCollector.Collect, cancellationTokenSource.Token),
                Task.Run(diskCollector.Collect, cancellationTokenSource.Token)
            ).ContinueWith(t => (t.Result[0], t.Result[1], t.Result[2]));

            stopwatch.Stop();
            var collectionTimeMs = (uint)stopwatch.ElapsedMilliseconds;

            // Post to pipeline
            var postSuccess = multiplexService.PostCpuMetrics(cpuData)
                            & multiplexService.PostNetworkMetrics(networkData.RxBytes, networkData.TxBytes, collectionTimeMs)
                            & multiplexService.PostDiskMetrics(diskData.ReadBytes, diskData.WriteBytes, diskData.ReadIops, diskData.WriteIops);

            if (!postSuccess)
                logger.LogWarning("Backpressure detected: some metrics not posted");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in metrics collection");
        }
    }
}, cancellationTokenSource.Token);

// Cleanup on shutdown
app.Lifetime.ApplicationStopping.Register(() =>
{
    cancellationTokenSource.Cancel();
    metricsTimer.Dispose();
    cancellationTokenSource.Dispose();
});

logger.LogInformation("Jetson Monitor Backend started");
logger.LogInformation("WebSocket endpoint: ws://localhost:5000/ws");
logger.LogInformation("Frontend: http://localhost:5000");

app.Run();
