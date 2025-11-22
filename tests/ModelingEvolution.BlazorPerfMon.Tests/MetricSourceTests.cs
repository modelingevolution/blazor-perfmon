using System.Text.Json;
using ModelingEvolution.BlazorPerfMon.Shared;

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
        Assert.Equal("\"Network:eth0/2\"", json);
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
        string json = "\"Network:eth0/2\"";

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
        Assert.Equal("Network:eth0/2", result);
    }

    [Theory]
    [InlineData("CPU/16", "CPU", null, 16u)]
    [InlineData("GPU/8", "GPU", null, 8u)]
    [InlineData("RAM/1", "RAM", null, 1u)]
    [InlineData("Network:eth0/2", "Network", "eth0", 2u)]
    [InlineData("Network:wlan0/2", "Network", "wlan0", 2u)]
    [InlineData("Disk:sda/2", "Disk", "sda", 2u)]
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
    [InlineData("CPU/")]
    [InlineData("CPU/abc")]
    [InlineData("CPU:eth0:extra/2")]
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
    [InlineData("Network:eth0/2", true)]
    [InlineData("CPU", true)]      // Valid: Name only, count defaults to 1
    [InlineData("invalid", true)]  // Valid: Name only, count defaults to 1
    [InlineData("", false)]
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

    // ========== Col-Span Feature Tests ==========

    [Fact]
    public void ToString_MetricWithColSpan_ProducesCorrectFormat()
    {
        // Arrange
        var metric = new MetricSource
        {
            Name = "Docker",
            Identifier = null,
            Count = 5,
            ColSpan = 2
        };

        // Act
        string result = metric.ToString();

        // Assert
        Assert.Equal("Docker/5|col-span:2", result);
    }

    [Fact]
    public void ToString_MetricWithIdentifierAndColSpan_ProducesCorrectFormat()
    {
        // Arrange
        var metric = new MetricSource
        {
            Name = "GPU",
            Identifier = "0",
            Count = 1,
            ColSpan = 3
        };

        // Act
        string result = metric.ToString();

        // Assert
        Assert.Equal("GPU:0/1|col-span:3", result);
    }

    [Fact]
    public void ToString_MetricWithDefaultColSpan_OmitsColSpanSection()
    {
        // Arrange
        var metric = new MetricSource
        {
            Name = "CPU",
            Identifier = null,
            Count = 16,
            ColSpan = 1
        };

        // Act
        string result = metric.ToString();

        // Assert
        Assert.Equal("CPU/16", result);
    }

    [Theory]
    [InlineData("CPU/16|col-span:2", "CPU", null, 16u, 2u)]
    [InlineData("Docker/5|col-span:3", "Docker", null, 5u, 3u)]
    [InlineData("GPU:0/1|col-span:4", "GPU", "0", 1u, 4u)]
    [InlineData("Network:eth0/2|col-span:1", "Network", "eth0", 2u, 1u)]
    [InlineData("RAM/1|col-span:12", "RAM", null, 1u, 12u)]
    public void Parse_ValidColSpanFormats_ProducesCorrectObjects(
        string input, string expectedName, string? expectedIdentifier, uint expectedCount, uint expectedColSpan)
    {
        // Act
        var metric = MetricSource.Parse(input, null);

        // Assert
        Assert.Equal(expectedName, metric.Name);
        Assert.Equal(expectedIdentifier, metric.Identifier);
        Assert.Equal(expectedCount, metric.Count);
        Assert.Equal(expectedColSpan, metric.ColSpan);
    }

    [Theory]
    [InlineData("CPU/16 | col-span:2", "CPU", null, 16u, 2u)]
    [InlineData("Docker / 5 | col-span : 3", "Docker", null, 5u, 3u)]
    [InlineData("GPU : 0 / 1 | col-span : 4", "GPU", "0", 1u, 4u)]
    [InlineData("  Network  :  eth0  /  2  |  col-span  :  5  ", "Network", "eth0", 2u, 5u)]
    [InlineData("RAM/1|  col-span:12", "RAM", null, 1u, 12u)]
    public void Parse_ColSpanWithWhitespace_ProducesCorrectObjects(
        string input, string expectedName, string? expectedIdentifier, uint expectedCount, uint expectedColSpan)
    {
        // Act
        var metric = MetricSource.Parse(input, null);

        // Assert
        Assert.Equal(expectedName, metric.Name);
        Assert.Equal(expectedIdentifier, metric.Identifier);
        Assert.Equal(expectedCount, metric.Count);
        Assert.Equal(expectedColSpan, metric.ColSpan);
    }

    [Theory]
    [InlineData("CPU/16|col-span:2")]
    [InlineData("CPU/16|Col-Span:2")]
    [InlineData("CPU/16|COL-SPAN:2")]
    [InlineData("CPU/16|CoL-SpAn:2")]
    public void Parse_ColSpanCaseInsensitive_ParsesSuccessfully(string input)
    {
        // Act
        var metric = MetricSource.Parse(input, null);

        // Assert
        Assert.Equal("CPU", metric.Name);
        Assert.Equal(2u, metric.ColSpan);
    }

    [Theory]
    [InlineData("CPU/16|col-span:0")]  // Below valid range
    [InlineData("CPU/16|col-span:13")] // Above valid range
    [InlineData("CPU/16|col-span:100")]
    [InlineData("CPU/16|col-span:-1")]
    public void Parse_ColSpanOutOfRange_ThrowsFormatException(string input)
    {
        // Act & Assert
        Assert.Throws<FormatException>(() => MetricSource.Parse(input, null));
    }

    [Theory]
    [InlineData("CPU/16|col-span:abc")] // Non-numeric col-span
    [InlineData("CPU/16|col-span:")]     // Empty col-span value
    [InlineData("CPU/16|invalid:2")]     // Wrong key
    [InlineData("CPU/16|col-span")]      // Missing colon
    [InlineData("CPU/16||col-span:2")]   // Double pipe
    [InlineData("CPU/16|col-span:2|extra")] // Multiple pipes
    public void Parse_InvalidColSpanFormats_ThrowsFormatException(string input)
    {
        // Act & Assert
        Assert.Throws<FormatException>(() => MetricSource.Parse(input, null));
    }

    [Fact]
    public void TryParse_ValidColSpan_ReturnsTrue()
    {
        // Act
        bool success = MetricSource.TryParse("CPU/16|col-span:2", null, out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("CPU", result.Name);
        Assert.Equal(16u, result.Count);
        Assert.Equal(2u, result.ColSpan);
    }

    [Theory]
    [InlineData("CPU/16|col-span:0")]
    [InlineData("CPU/16|col-span:13")]
    [InlineData("CPU/16|invalid:2")]
    [InlineData("CPU/16|col-span:abc")]
    public void TryParse_InvalidColSpan_ReturnsFalse(string input)
    {
        // Act
        bool success = MetricSource.TryParse(input, null, out var result);

        // Assert
        Assert.False(success);
        Assert.Equal(default, result);
    }

    [Fact]
    public void JsonRoundTrip_MetricWithColSpan_PreservesValues()
    {
        // Arrange
        var original = new MetricSource
        {
            Name = "Docker",
            Identifier = null,
            Count = 5,
            ColSpan = 2
        };

        // Act
        string json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<MetricSource>(json);

        // Assert
        Assert.Equal(original.Name, deserialized.Name);
        Assert.Equal(original.Identifier, deserialized.Identifier);
        Assert.Equal(original.Count, deserialized.Count);
        Assert.Equal(original.ColSpan, deserialized.ColSpan);
    }

    [Fact]
    public void JsonSerialization_MetricWithColSpan_ProducesCompactString()
    {
        // Arrange
        var metric = new MetricSource
        {
            Name = "Docker",
            Identifier = null,
            Count = 5,
            ColSpan = 2
        };

        // Act
        string json = JsonSerializer.Serialize(metric);

        // Assert
        Assert.Equal("\"Docker/5|col-span:2\"", json);
    }

    [Fact]
    public void JsonDeserialization_CompactStringWithColSpan_CreatesCorrectObject()
    {
        // Arrange
        string json = "\"Docker/5|col-span:2\"";

        // Act
        var metric = JsonSerializer.Deserialize<MetricSource>(json);

        // Assert
        Assert.Equal("Docker", metric.Name);
        Assert.Null(metric.Identifier);
        Assert.Equal(5u, metric.Count);
        Assert.Equal(2u, metric.ColSpan);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(6)]
    [InlineData(12)]
    public void Parse_ColSpanInValidRange_ParsesSuccessfully(uint colSpan)
    {
        // Arrange
        string input = $"CPU/16|col-span:{colSpan}";

        // Act
        var metric = MetricSource.Parse(input, null);

        // Assert
        Assert.Equal(colSpan, metric.ColSpan);
    }

    [Fact]
    public void ParseToStringRoundTrip_WithColSpan_PreservesFormat()
    {
        // Arrange
        string original = "Docker/5|col-span:2";

        // Act
        var metric = MetricSource.Parse(original, null);
        string result = metric.ToString();

        // Assert
        Assert.Equal(original, result);
    }

    [Fact]
    public void ParseToStringRoundTrip_WithIdentifierAndColSpan_PreservesFormat()
    {
        // Arrange
        string original = "GPU:0/1|col-span:3";

        // Act
        var metric = MetricSource.Parse(original, null);
        string result = metric.ToString();

        // Assert
        Assert.Equal(original, result);
    }

    [Fact]
    public void Parse_DefaultColSpanNotSpecified_DefaultsToOne()
    {
        // Arrange
        string input = "CPU/16";

        // Act
        var metric = MetricSource.Parse(input, null);

        // Assert
        Assert.Equal(1u, metric.ColSpan);
    }
}
