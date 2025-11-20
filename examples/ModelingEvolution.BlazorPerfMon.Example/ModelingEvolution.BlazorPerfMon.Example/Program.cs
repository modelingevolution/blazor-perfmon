using Backend.Collectors;
using Backend.Core;
using Backend.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Configure settings
builder.Services.Configure<MonitorSettings>(
    builder.Configuration.GetSection("MonitorSettings"));

// Register Backend services as singletons
builder.Services.AddSingleton<CpuCollector>();
builder.Services.AddSingleton<NetworkCollector>();
builder.Services.AddSingleton<DiskCollector>();
builder.Services.AddSingleton<MultiplexService>();
builder.Services.AddSingleton<WebSocketService>();

// Register GPU collector based on configuration
builder.Services.AddSingleton<IGpuCollector>(sp =>
{
    var settingsOptions = sp.GetRequiredService<IOptions<MonitorSettings>>();
    var settings = settingsOptions.Value;
    var logger = sp.GetRequiredService<ILoggerFactory>();

    return settings.GpuCollectorType.ToLowerInvariant() switch
    {
        "nvtegra" => new NvTegraGpuCollector(logger.CreateLogger<NvTegraGpuCollector>()),
        "nvsmi" => new NvSmiGpuCollector(logger.CreateLogger<NvSmiGpuCollector>()),
        "nvml" => new NvmlGpuCollector(logger.CreateLogger<NvmlGpuCollector>(), settingsOptions),
        _ => new NvmlGpuCollector(logger.CreateLogger<NvmlGpuCollector>(), settingsOptions)
    };
});

// Add Blazor services
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

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

// Map Razor components
app.MapStaticAssets();
app.MapRazorComponents<ModelingEvolution.BlazorPerfMon.Example.Components.App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(ModelingEvolution.BlazorPerfMon.Example.Client._Imports).Assembly);

// Start metrics collection using PeriodicTimer
var cpuCollector = app.Services.GetRequiredService<CpuCollector>();
var networkCollector = app.Services.GetRequiredService<NetworkCollector>();
var diskCollector = app.Services.GetRequiredService<DiskCollector>();
var gpuCollector = app.Services.GetRequiredService<IGpuCollector>();
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

            // Collect metrics in parallel with individual timing
            var cpuSw = System.Diagnostics.Stopwatch.StartNew();
            var cpuTask = Task.Run(() => { var result = cpuCollector.Collect(); cpuSw.Stop(); return result; }, cancellationTokenSource.Token);

            var gpuSw = System.Diagnostics.Stopwatch.StartNew();
            var gpuTask = Task.Run(() => { var result = gpuCollector.Collect(); gpuSw.Stop(); return result; }, cancellationTokenSource.Token);

            var networkSw = System.Diagnostics.Stopwatch.StartNew();
            var networkTask = Task.Run(() => { var result = networkCollector.Collect(); networkSw.Stop(); return result; }, cancellationTokenSource.Token);

            var diskSw = System.Diagnostics.Stopwatch.StartNew();
            var diskTask = Task.Run(() => { var result = diskCollector.Collect(); diskSw.Stop(); return result; }, cancellationTokenSource.Token);

            await Task.WhenAll(cpuTask, gpuTask, networkTask, diskTask);

            stopwatch.Stop();
            var collectionTimeMs = (uint)stopwatch.ElapsedMilliseconds;

            // Log individual collector timings when total exceeds interval
            if (collectionTimeMs > settings.CollectionIntervalMs)
            {
                logger.LogWarning("Collection time {CollectionTimeMs}ms exceeds interval {IntervalMs}ms. CPU: {CpuMs}ms, GPU: {GpuMs}ms, Network: {NetworkMs}ms, Disk: {DiskMs}ms",
                    collectionTimeMs, settings.CollectionIntervalMs,
                    cpuSw.ElapsedMilliseconds, gpuSw.ElapsedMilliseconds,
                    networkSw.ElapsedMilliseconds, diskSw.ElapsedMilliseconds);
            }

            // Post to pipeline
            var postSuccess = multiplexService.PostCpuGpuMetrics(cpuTask.Result, gpuTask.Result)
                            & multiplexService.PostNetworkMetrics(networkTask.Result.RxBytes, networkTask.Result.TxBytes, collectionTimeMs)
                            & multiplexService.PostDiskMetrics(diskTask.Result.ReadBytes, diskTask.Result.WriteBytes, diskTask.Result.ReadIops, diskTask.Result.WriteIops);

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

logger.LogInformation("Blazor PerfMon Example started");
logger.LogInformation("WebSocket endpoint: ws://localhost:5000/ws");
logger.LogInformation("Frontend: http://localhost:5000");

app.Run();
