using System.Text.Json;
using Frontend.Models;

namespace ModelingEvolution.BlazorPerfMon.Tests;

/// <summary>
/// Tests for MetricSource struct, particularly JSON serialization with JsonParsableConverter.
/// </summary>
public class MetricSourceTests
{
    [Fact]
    public void JsonRoundTrip_SimpleMetric_PreservesValues()
    {
        // Arrange
        var original = new MetricSource
        {
            Name = "CPU",
            Identifier = null,
            Count = 16
        };

        // Act
        string json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<MetricSource>(json);

        // Assert
        Assert.Equal(original.Name, deserialized.Name);
        Assert.Equal(original.Identifier, deserialized.Identifier);
        Assert.Equal(original.Count, deserialized.Count);
    }

    [Fact]
    public void JsonRoundTrip_MetricWithIdentifier_PreservesValues()
    {
        // Arrange
        var original = new MetricSource
        {
            Name = "Network",
            Identifier = "eth0",
            Count = 2
        };

        // Act
        string json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<MetricSource>(json);

        // Assert
        Assert.Equal(original.Name, deserialized.Name);
        Assert.Equal(original.Identifier, deserialized.Identifier);
        Assert.Equal(original.Count, deserialized.Count);
    }

    [Fact]
    public void JsonSerialization_SimpleMetric_ProducesCompactString()
    {
        // Arrange
        var metric = new MetricSource
        {
            Name = "CPU",
            Identifier = null,
            Count = 16
        };

        // Act
        string json = JsonSerializer.Serialize(metric);

        // Assert
        // Should serialize as compact string using IParsable format
        Assert.Equal("\"CPU/16\"", json);
    }

    [Fact]
    public void JsonSerialization_MetricWithIdentifier_ProducesCompactString()
    {
        // Arrange
        var metric = new MetricSource
        {
            Name = "Network",
            Identifier = "eth0",
            Count = 2
        };

        // Act
        string json = JsonSerializer.Serialize(metric);

        // Assert
        // Should serialize as compact string using IParsable format
        Assert.Equal("\"Network.eth0/2\"", json);
    }

    [Fact]
    public void JsonDeserialization_CompactString_CreatesCorrectObject()
    {
        // Arrange
        string json = "\"CPU/16\"";

        // Act
        var metric = JsonSerializer.Deserialize<MetricSource>(json);

        // Assert
        Assert.Equal("CPU", metric.Name);
        Assert.Null(metric.Identifier);
        Assert.Equal(16u, metric.Count);
    }

    [Fact]
    public void JsonDeserialization_CompactStringWithIdentifier_CreatesCorrectObject()
    {
        // Arrange
        string json = "\"Network.eth0/2\"";

        // Act
        var metric = JsonSerializer.Deserialize<MetricSource>(json);

        // Assert
        Assert.Equal("Network", metric.Name);
        Assert.Equal("eth0", metric.Identifier);
        Assert.Equal(2u, metric.Count);
    }

    [Fact]
    public void JsonRoundTrip_Array_PreservesAllElements()
    {
        // Arrange
        var original = new[]
        {
            new MetricSource { Name = "CPU", Identifier = null, Count = 16 },
            new MetricSource { Name = "GPU", Identifier = null, Count = 8 },
            new MetricSource { Name = "Network", Identifier = "eth0", Count = 2 },
            new MetricSource { Name = "Network", Identifier = "eth1", Count = 2 }
        };

        // Act
        string json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<MetricSource[]>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.Length, deserialized.Length);

        for (int i = 0; i < original.Length; i++)
        {
            Assert.Equal(original[i].Name, deserialized[i].Name);
            Assert.Equal(original[i].Identifier, deserialized[i].Identifier);
            Assert.Equal(original[i].Count, deserialized[i].Count);
        }
    }

    [Fact]
    public void ToString_SimpleMetric_ProducesCorrectFormat()
    {
        // Arrange
        var metric = new MetricSource
        {
            Name = "CPU",
            Identifier = null,
            Count = 16
        };

        // Act
        string result = metric.ToString();

        // Assert
        Assert.Equal("CPU/16", result);
    }

    [Fact]
    public void ToString_MetricWithIdentifier_ProducesCorrectFormat()
    {
        // Arrange
        var metric = new MetricSource
        {
            Name = "Network",
            Identifier = "eth0",
            Count = 2
        };

        // Act
        string result = metric.ToString();

        // Assert
        Assert.Equal("Network.eth0/2", result);
    }

    [Theory]
    [InlineData("CPU/16", "CPU", null, 16u)]
    [InlineData("GPU/8", "GPU", null, 8u)]
    [InlineData("RAM/1", "RAM", null, 1u)]
    [InlineData("Network.eth0/2", "Network", "eth0", 2u)]
    [InlineData("Network.wlan0/2", "Network", "wlan0", 2u)]
    [InlineData("Disk.sda/2", "Disk", "sda", 2u)]
    public void Parse_ValidFormats_ProducesCorrectObjects(string input, string expectedName, string? expectedIdentifier, uint expectedCount)
    {
        // Act
        var metric = MetricSource.Parse(input, null);

        // Assert
        Assert.Equal(expectedName, metric.Name);
        Assert.Equal(expectedIdentifier, metric.Identifier);
        Assert.Equal(expectedCount, metric.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("CPU")]
    [InlineData("CPU/")]
    [InlineData("CPU/abc")]
    [InlineData("CPU.eth0.extra/2")]
    public void Parse_InvalidFormats_ThrowsFormatException(string input)
    {
        // Act & Assert
        Assert.Throws<FormatException>(() => MetricSource.Parse(input, null));
    }

    [Fact]
    public void Parse_EmptyName_AllowedByCurrentImplementation()
    {
        // Arrange
        string input = "/16";

        // Act - currently allowed by implementation (Name = "")
        var metric = MetricSource.Parse(input, null);

        // Assert
        Assert.Equal("", metric.Name);
        Assert.Null(metric.Identifier);
        Assert.Equal(16u, metric.Count);
    }

    [Theory]
    [InlineData("CPU/16", true)]
    [InlineData("Network.eth0/2", true)]
    [InlineData("", false)]
    [InlineData("invalid", false)]
    [InlineData("CPU/", false)]
    public void TryParse_VariousInputs_ReturnsExpectedResult(string input, bool expectedSuccess)
    {
        // Act
        bool success = MetricSource.TryParse(input, null, out var result);

        // Assert
        Assert.Equal(expectedSuccess, success);

        if (expectedSuccess)
        {
            Assert.NotEqual(default, result);
        }
    }
}
