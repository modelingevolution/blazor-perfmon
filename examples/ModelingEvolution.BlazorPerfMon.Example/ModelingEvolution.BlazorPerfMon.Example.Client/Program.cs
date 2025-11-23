using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ModelingEvolution.BlazorPerfMon.Client.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Calculate WebSocket URL from base address
string wsUrl = builder.HostEnvironment.BaseAddress
    .Replace("http://", "ws://")
    .Replace("https://", "wss://") + "ws";

// Add Performance Monitor client services (component creates its own service instances for proper disposal)
builder.Services.AddPerformanceMonitorClient(wsUrl, dataPointsToKeep: 120);

await builder.Build().RunAsync();
