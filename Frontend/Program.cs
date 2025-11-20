using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Options;
using Frontend;
using Frontend.Services;
using Frontend.Models;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure settings
builder.Services.Configure<MonitorSettings>(
    builder.Configuration.GetSection("MonitorSettings"));

// Register services as singletons
builder.Services.AddSingleton<MetricsStore>();

// WebSocket URL configuration
// In production on Jetson: ws://localhost:5000/ws
// For development: ws://localhost:5000/ws
string wsUrl = builder.HostEnvironment.BaseAddress.Replace("http://", "ws://").Replace("https://", "wss://") + "ws";
builder.Services.AddSingleton(sp => new WebSocketClient(wsUrl));

Console.WriteLine($"Frontend initialized. WebSocket URL: {wsUrl}");

await builder.Build().RunAsync();
