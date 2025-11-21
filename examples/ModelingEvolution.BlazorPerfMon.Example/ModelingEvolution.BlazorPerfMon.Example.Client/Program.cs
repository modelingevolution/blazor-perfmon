using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Frontend.Services;
using Frontend.Models;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Configure settings
builder.Services.Configure<MonitorSettings>(
    builder.Configuration.GetSection("MonitorSettings"));

// Register services as singletons
builder.Services.AddSingleton<MetricsStore>(sp => new MetricsStore(capacity: 120)); // 60 seconds at 2Hz

// WebSocket URL configuration
string wsUrl = builder.HostEnvironment.BaseAddress.Replace("http://", "ws://").Replace("https://", "wss://") + "ws";
builder.Services.AddSingleton(sp => new WebSocketClient(wsUrl));

Console.WriteLine($"Frontend initialized. WebSocket URL: {wsUrl}");

await builder.Build().RunAsync();
