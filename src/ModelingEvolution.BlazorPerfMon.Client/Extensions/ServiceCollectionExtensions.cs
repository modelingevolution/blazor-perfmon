using Microsoft.Extensions.DependencyInjection;

namespace ModelingEvolution.BlazorPerfMon.Client.Extensions;

/// <summary>
/// Configuration options for Performance Monitor client.
/// </summary>
public class PerformanceMonitorClientOptions
{
    /// <summary>
    /// WebSocket URL to connect to.
    /// </summary>
    public string WebSocketUrl { get; set; } = string.Empty;

    /// <summary>
    /// Number of data points to retain in the rolling window (default: 120).
    /// </summary>
    public int DataPointsToKeep { get; set; } = 120;
}

/// <summary>
/// Extension methods for configuring Performance Monitor client services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Performance Monitor client configuration to the service collection.
    /// Components create their own service instances for proper disposal/recreation support.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="wsUrl">The WebSocket URL to connect to</param>
    /// <param name="dataPointsToKeep">Number of data points to retain in the rolling window (default: 120)</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddPerformanceMonitorClient(
        this IServiceCollection services,
        string wsUrl,
        int dataPointsToKeep = 120)
    {
        // Register client options as singleton (configuration only)
        services.AddSingleton(new PerformanceMonitorClientOptions
        {
            WebSocketUrl = wsUrl,
            DataPointsToKeep = dataPointsToKeep
        });

        Console.WriteLine($"PerformanceMonitor client configured. WebSocket URL: {wsUrl}, DataPoints: {dataPointsToKeep}");

        return services;
    }

    /// <summary>
    /// Adds Performance Monitor client configuration with options.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="options">Client configuration options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddPerformanceMonitorClient(
        this IServiceCollection services,
        PerformanceMonitorClientOptions options)
    {
        services.AddSingleton(options);
        Console.WriteLine($"PerformanceMonitor client configured. WebSocket URL: {options.WebSocketUrl}, DataPoints: {options.DataPointsToKeep}");
        return services;
    }
}

