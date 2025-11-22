using ModelingEvolution.BlazorPerfMon.Client.Services;
using ModelingEvolution.BlazorPerfMon.Server.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register Performance Monitor services
builder.Services.AddPerformanceMonitor(builder.Configuration);

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
}

// HTTP only - no HTTPS redirection

// Configure middleware
app.UseRouting();
app.UseAntiforgery();

// Map Performance Monitor WebSocket endpoint
app.MapPerformanceMonitorEndpoint();

// Map Razor components
app.MapStaticAssets();
app.MapRazorComponents<ModelingEvolution.BlazorPerfMon.Example.Components.App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(
        typeof(ModelingEvolution.BlazorPerfMon.Example.Client._Imports).Assembly,
        typeof(WebSocketClient).Assembly);

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Blazor PerfMon Example started");
logger.LogInformation("WebSocket endpoint: ws://localhost:5000/ws");
logger.LogInformation("ModelingEvolution.BlazorPerfMon.Client: http://localhost:5000");

app.Run();
