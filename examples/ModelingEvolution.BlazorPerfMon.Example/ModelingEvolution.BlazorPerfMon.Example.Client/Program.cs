using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ModelingEvolution.BlazorPerfMon.Client.Services;
using ModelingEvolution.BlazorPerfMon.Client.Models;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Configure settings
builder.Services.Configure<MonitorSettings>(
    builder.Configuration.GetSection("MonitorSettings"));

// Register services as singletons
builder.Services.AddSingleton<MetricsStore>(sp => new MetricsStore(intervals: 120)); // 60 seconds: 120 intervals × 500ms

// WebSocket URL configuration
string wsUrl = builder.HostEnvironment.BaseAddress.Replace("http://", "ws://").Replace("https://", "wss://") + "ws";
builder.Services.AddSingleton(sp => new WebSocketClient(wsUrl));

Console.WriteLine($"ModelingEvolution.BlazorPerfMon.Client initialized. WebSocket URL: {wsUrl}");

await builder.Build().RunAsync();
