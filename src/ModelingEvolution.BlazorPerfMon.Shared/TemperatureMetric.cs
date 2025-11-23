using MessagePack;

namespace ModelingEvolution.BlazorPerfMon.Shared;

/// <summary>
/// Temperature metric for a single thermal sensor.
/// MessagePack-serializable structure for transmission over WebSocket.
/// </summary>
[MessagePackObject]
public readonly record struct TemperatureMetric
{
    /// <summary>
    /// Sensor identifier (e.g., "cpu", "gpu", "cv0", "soc0", "tj").
    /// </summary>
    [Key(0)]
    public string Sensor { get; init; }

    /// <summary>
    /// Temperature in Celsius.
    /// </summary>
    [Key(1)]
    public float TempCelsius { get; init; }
}
