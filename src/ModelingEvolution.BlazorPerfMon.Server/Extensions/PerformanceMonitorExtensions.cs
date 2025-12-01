using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelingEvolution.BlazorPerfMon.Server.Collectors;
using ModelingEvolution.BlazorPerfMon.Server.Core;
using ModelingEvolution.BlazorPerfMon.Server.Services;

namespace ModelingEvolution.BlazorPerfMon.Server.Extensions;

/// <summary>
/// Extension methods for registering and configuring the Performance Monitor.
/// </summary>
public static class PerformanceMonitorExtensions
{
    /// <summary>
    /// Registers all Performance Monitor services (collectors, multiplex service, WebSocket service, engine).
    /// GPU collector type is determined by MonitorSettings.GpuCollectorType ("nvml", "nvsmi", "nvtegra", or "none").
    /// </summary>
    public static IServiceCollection AddPerformanceMonitor(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure settings
        services.Configure<MonitorSettings>(
            configuration.GetSection("MonitorSettings"));

        // Register collectors as singletons
        services.AddSingleton<CpuCollector>();
        services.AddSingleton<RamCollector>();
        services.AddSingleton<NetworkCollector>();
        services.AddSingleton<DiskCollector>();
        services.AddSingleton<DockerCollector>();

        // Register GPU collector based on configuration
        services.AddSingleton<IGpuCollector>(sp =>
        {
            var settingsOptions = sp.GetRequiredService<IOptions<MonitorSettings>>();
            var settings = settingsOptions.Value;
            var logger = sp.GetRequiredService<ILoggerFactory>();

            return settings.GpuCollectorType.ToLowerInvariant() switch
            {
                "nvtegra" => new NvTegraGpuCollector(logger.CreateLogger<NvTegraGpuCollector>()),
                "nvsmi" => new NvSmiGpuCollector(logger.CreateLogger<NvSmiGpuCollector>()),
                "nvml" => CreateNvmlCollector(logger, settingsOptions),
                "none" => new NoOpGpuCollector(),
                _ => new NvSmiGpuCollector(logger.CreateLogger<NvSmiGpuCollector>()) // Default to NvSmi (safest option)
            };
        });

        // Register services
        services.AddSingleton<MultiplexService>(sp =>
        {
            var gpuCollector = sp.GetRequiredService<IGpuCollector>();
            // All GPU collectors now also implement ITemperatureCollector
            var temperatureCollector = gpuCollector as ITemperatureCollector;
            return new MultiplexService(gpuCollector, temperatureCollector);
        });
        services.AddSingleton<MetricsConfigurationBuilder>();
        services.AddSingleton<WebSocketService>();
        services.AddSingleton<PerformanceMonitorEngine>();

        return services;
    }

    /// <summary>
    /// Creates NvmlGpuCollector in a separate method to prevent assembly loading unless explicitly used.
    /// This isolation ensures ManagedNvml assembly is only loaded when "nvml" is configured.
    /// </summary>
    private static IGpuCollector CreateNvmlCollector(ILoggerFactory loggerFactory, IOptions<MonitorSettings> settings)
    {
        return new NvmlGpuCollector(loggerFactory.CreateLogger<NvmlGpuCollector>(), settings);
    }

    /// <summary>
    /// Maps the Performance Monitor WebSocket endpoint at /ws.
    /// Configures WebSockets middleware and wires up the engine to start/stop based on client connections.
    /// The metrics collection engine only runs when there is at least one connected WebSocket client.
    /// </summary>
    public static IApplicationBuilder MapPerformanceMonitorEndpoint(this IApplicationBuilder app)
    {
        // Enable WebSockets
        app.UseWebSockets(new WebSocketOptions
        {
            KeepAliveInterval = TimeSpan.FromSeconds(30)
        });

        // Get services
        var multiplexService = app.ApplicationServices.GetRequiredService<MultiplexService>();
        var engine = app.ApplicationServices.GetRequiredService<PerformanceMonitorEngine>();

        // Wire up engine to start/stop based on client connections
        multiplexService.FirstClientConnected += () =>
        {
            engine.Start();
        };

        multiplexService.LastClientDisconnected += () =>
        {
            engine.Stop();
        };

        // WebSocket endpoint for metrics streaming
        app.Map("/ws", wsApp =>
        {
            wsApp.Run(async context =>
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
        });

        return app;
    }
}
