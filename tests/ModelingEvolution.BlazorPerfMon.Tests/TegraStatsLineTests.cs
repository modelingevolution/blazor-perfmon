using System.Collections.Immutable;
using System.Globalization;
using ModelingEvolution.BlazorPerfMon.Server.Collectors;

namespace ModelingEvolution.BlazorPerfMon.Tests;

/// <summary>
/// Comprehensive tests for TegraStatsLine and related parsing types.
/// </summary>
public class TegraStatsLineTests
{
    private const string SampleTegraStatsLine =
        "11-23-2025 22:47:56 RAM 1729/15657MB (lfb 8x4MB) SWAP 0/7828MB (cached 0MB) " +
        "CPU [1%@1420,2%@1420,4%@1420,3%@1420,off,off,off,off] GR3D_FREQ 0% " +
        "cv0@43.562C cpu@46.125C soc2@43.781C soc0@44.656C cv1@43.312C gpu@43C tj@46.937C soc1@46.937C cv2@43.781C " +
        "VDD_IN 6120mW/6120mW VDD_CPU_GPU_CV 436mW/436mW VDD_SOC 2464mW/2464mW";

    #region RamInfo Tests

    [Theory]
    [InlineData("1729/15657MB (lfb 8x4MB)", 1729, 15657, 8, 4)]
    [InlineData("0/32000MB (lfb 16x2MB)", 0, 32000, 16, 2)]
    [InlineData("16000/16000MB (lfb 1x1MB)", 16000, 16000, 1, 1)]
    public void RamInfo_Parse_ValidInput_ParsesCorrectly(string input, int expectedUsed, int expectedTotal, int expectedLfbCount, int expectedLfbSize)
    {
        // Act
        var result = RamInfo.Parse(input);

        // Assert
        Assert.Equal(expectedUsed, result.UsedMB);
        Assert.Equal(expectedTotal, result.TotalMB);
        Assert.Equal(expectedLfbCount, result.LowFragBufferCount);
        Assert.Equal(expectedLfbSize, result.LowFragBufferSizeMB);
    }

    [Fact]
    public void RamInfo_UsagePercent_CalculatesCorrectly()
    {
        // Arrange
        var ramInfo = new RamInfo { UsedMB = 1729, TotalMB = 15657 };

        // Act
        var percent = ramInfo.UsagePercent;

        // Assert
        Assert.InRange(percent, 11.0f, 11.1f); // Approximately 11.04%
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("1729/15657MB")]
    [InlineData("(lfb 8x4MB)")]
    [InlineData("1729/15657 (lfb 8x4MB)")]
    public void RamInfo_Parse_InvalidInput_ThrowsFormatException(string input)
    {
        // Act & Assert
        Assert.Throws<FormatException>(() => RamInfo.Parse(input));
    }

    [Fact]
    public void RamInfo_TryParse_ValidInput_ReturnsTrue()
    {
        // Arrange
        string input = "1729/15657MB (lfb 8x4MB)";

        // Act
        bool success = RamInfo.TryParse(input, null, out var result);

        // Assert
        Assert.True(success);
        Assert.Equal(1729, result.UsedMB);
    }

    [Fact]
    public void RamInfo_TryParse_InvalidInput_ReturnsFalse()
    {
        // Arrange
        string input = "invalid";

        // Act
        bool success = RamInfo.TryParse(input, null, out var result);

        // Assert
        Assert.False(success);
        Assert.Equal(default, result);
    }

    #endregion

    #region SwapInfo Tests

    [Theory]
    [InlineData("0/7828MB (cached 0MB)", 0, 7828, 0)]
    [InlineData("1024/8192MB (cached 512MB)", 1024, 8192, 512)]
    [InlineData("4096/4096MB (cached 2048MB)", 4096, 4096, 2048)]
    public void SwapInfo_Parse_ValidInput_ParsesCorrectly(string input, int expectedUsed, int expectedTotal, int expectedCached)
    {
        // Act
        var result = SwapInfo.Parse(input);

        // Assert
        Assert.Equal(expectedUsed, result.UsedMB);
        Assert.Equal(expectedTotal, result.TotalMB);
        Assert.Equal(expectedCached, result.CachedMB);
    }

    [Fact]
    public void SwapInfo_UsagePercent_CalculatesCorrectly()
    {
        // Arrange
        var swapInfo = new SwapInfo { UsedMB = 1024, TotalMB = 8192 };

        // Act
        var percent = swapInfo.UsagePercent;

        // Assert
        Assert.Equal(12.5f, percent);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0/7828MB")]
    [InlineData("cached 0MB")]
    [InlineData("0/7828 (cached 0MB)")]
    public void SwapInfo_Parse_InvalidInput_ThrowsFormatException(string input)
    {
        // Act & Assert
        Assert.Throws<FormatException>(() => SwapInfo.Parse(input));
    }

    #endregion

    #region CpuCoreState Tests

    [Theory]
    [InlineData("1%@1420", true, 1, 1420)]
    [InlineData("100%@2000", true, 100, 2000)]
    [InlineData("0%@729", true, 0, 729)]
    [InlineData("50%@1500", true, 50, 1500)]
    public void CpuCoreState_Parse_OnlineCore_ParsesCorrectly(string input, bool expectedOnline, int expectedUtil, int expectedFreq)
    {
        // Act
        var result = CpuCoreState.Parse(input);

        // Assert
        Assert.Equal(expectedOnline, result.IsOnline);
        Assert.Equal(expectedUtil, result.UtilizationPercent);
        Assert.Equal(expectedFreq, result.FrequencyMHz);
    }

    [Theory]
    [InlineData("off")]
    [InlineData("OFF")]
    [InlineData("Off")]
    [InlineData("  off  ")]
    public void CpuCoreState_Parse_OfflineCore_ParsesCorrectly(string input)
    {
        // Act
        var result = CpuCoreState.Parse(input);

        // Assert
        Assert.False(result.IsOnline);
        Assert.Equal(0, result.UtilizationPercent);
        Assert.Equal(0, result.FrequencyMHz);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("50%")]
    [InlineData("@1500")]
    [InlineData("50@1500")]
    public void CpuCoreState_Parse_InvalidInput_ThrowsFormatException(string input)
    {
        // Act & Assert
        Assert.Throws<FormatException>(() => CpuCoreState.Parse(input));
    }

    #endregion

    #region CpuInfo Tests

    [Fact]
    public void CpuInfo_Parse_MixedCores_ParsesCorrectly()
    {
        // Arrange
        string input = "[1%@1420,2%@1420,4%@1420,3%@1420,off,off,off,off]";

        // Act
        var result = CpuInfo.Parse(input);

        // Assert
        Assert.Equal(8, result.Cores.Length);
        Assert.Equal(4, result.OnlineCoreCount);

        // Check first core
        Assert.True(result.Cores[0].IsOnline);
        Assert.Equal(1, result.Cores[0].UtilizationPercent);
        Assert.Equal(1420, result.Cores[0].FrequencyMHz);

        // Check offline core
        Assert.False(result.Cores[4].IsOnline);
    }

    [Fact]
    public void CpuInfo_AverageUtilization_CalculatesCorrectly()
    {
        // Arrange
        string input = "[10%@1420,20%@1420,30%@1420,40%@1420,off,off,off,off]";
        var cpuInfo = CpuInfo.Parse(input);

        // Act
        var avgUtil = cpuInfo.AverageUtilization;

        // Assert
        Assert.Equal(25f, avgUtil); // (10+20+30+40)/4 = 25
    }

    [Fact]
    public void CpuInfo_Parse_AllCoresOnline_ParsesCorrectly()
    {
        // Arrange
        string input = "[50%@2000,60%@2000,70%@2000,80%@2000]";

        // Act
        var result = CpuInfo.Parse(input);

        // Assert
        Assert.Equal(4, result.Cores.Length);
        Assert.Equal(4, result.OnlineCoreCount);
        Assert.All(result.Cores, core => Assert.True(core.IsOnline));
    }

    [Fact]
    public void CpuInfo_Parse_AllCoresOffline_ParsesCorrectly()
    {
        // Arrange
        string input = "[off,off,off,off]";

        // Act
        var result = CpuInfo.Parse(input);

        // Assert
        Assert.Equal(4, result.Cores.Length);
        Assert.Equal(0, result.OnlineCoreCount);
        Assert.All(result.Cores, core => Assert.False(core.IsOnline));
        Assert.Equal(0f, result.AverageUtilization);
    }

    [Theory]
    [InlineData("")]
    [InlineData("50%@1500")]
    [InlineData("[")]
    [InlineData("]")]
    [InlineData("[invalid]")]
    public void CpuInfo_Parse_InvalidInput_ThrowsFormatException(string input)
    {
        // Act & Assert
        Assert.Throws<FormatException>(() => CpuInfo.Parse(input));
    }

    #endregion

    #region TemperatureReading Tests

    [Theory]
    [InlineData("cv0@43.562C", "cv0", 43.562f)]
    [InlineData("cpu@46.125C", "cpu", 46.125f)]
    [InlineData("gpu@43C", "gpu", 43f)]
    [InlineData("tj@46.937C", "tj", 46.937f)]
    [InlineData("soc0@44.656C", "soc0", 44.656f)]
    public void TemperatureReading_Parse_ValidInput_ParsesCorrectly(string input, string expectedSensor, float expectedTemp)
    {
        // Act
        var result = TemperatureReading.Parse(input);

        // Assert
        Assert.Equal(expectedSensor, result.Sensor);
        Assert.Equal(expectedTemp, result.TempCelsius, precision: 3);
    }

    [Theory]
    [InlineData("")]
    [InlineData("cv0@43")]
    [InlineData("cv0@C")]
    [InlineData("cv0:43C")]
    [InlineData("@43C")]
    public void TemperatureReading_Parse_InvalidInput_ThrowsFormatException(string input)
    {
        // Act & Assert
        Assert.Throws<FormatException>(() => TemperatureReading.Parse(input));
    }

    #endregion

    #region PowerRailReading Tests

    [Theory]
    [InlineData("VDD_IN 6120mW/6120mW", "VDD_IN", 6120, 6120)]
    [InlineData("VDD_CPU_GPU_CV 436mW/436mW", "VDD_CPU_GPU_CV", 436, 436)]
    [InlineData("VDD_SOC 2464mW/2464mW", "VDD_SOC", 2464, 2464)]
    [InlineData("VDD_IN 5000mW/6000mW", "VDD_IN", 5000, 6000)]
    public void PowerRailReading_Parse_ValidInput_ParsesCorrectly(string input, string expectedName, int expectedCurrent, int expectedAverage)
    {
        // Act
        var result = PowerRailReading.Parse(input);

        // Assert
        Assert.Equal(expectedName, result.Name);
        Assert.Equal(expectedCurrent, result.CurrentMilliwatts);
        Assert.Equal(expectedAverage, result.AverageMilliwatts);
    }

    [Theory]
    [InlineData("")]
    [InlineData("VDD_IN 6120mW")]
    [InlineData("VDD_IN 6120/6120")]
    [InlineData("6120mW/6120mW")]
    [InlineData("VDD_IN 6120mW/6120W")]
    public void PowerRailReading_Parse_InvalidInput_ThrowsFormatException(string input)
    {
        // Act & Assert
        Assert.Throws<FormatException>(() => PowerRailReading.Parse(input));
    }

    #endregion

    #region TegraStatsLine Complete Tests

    [Fact]
    public void TegraStatsLine_Parse_CompleteLine_ParsesAllFieldsCorrectly()
    {
        // Act
        var result = TegraStatsLine.Parse(SampleTegraStatsLine);

        // Assert - Timestamp
        Assert.Equal(new DateTime(2025, 11, 23, 22, 47, 56), result.Timestamp);

        // Assert - RAM
        Assert.Equal(1729, result.Ram.UsedMB);
        Assert.Equal(15657, result.Ram.TotalMB);
        Assert.Equal(8, result.Ram.LowFragBufferCount);
        Assert.Equal(4, result.Ram.LowFragBufferSizeMB);

        // Assert - SWAP
        Assert.Equal(0, result.Swap.UsedMB);
        Assert.Equal(7828, result.Swap.TotalMB);
        Assert.Equal(0, result.Swap.CachedMB);

        // Assert - CPU
        Assert.Equal(8, result.Cpu.Cores.Length);
        Assert.Equal(4, result.Cpu.OnlineCoreCount);
        Assert.Equal(1, result.Cpu.Cores[0].UtilizationPercent);
        Assert.Equal(1420, result.Cpu.Cores[0].FrequencyMHz);
        Assert.False(result.Cpu.Cores[4].IsOnline);

        // Assert - GPU
        Assert.Equal(0, result.GpuUtilizationPercent);

        // Assert - Temperatures
        Assert.Equal(9, result.Temperatures.Length);
        Assert.Contains(result.Temperatures, t => t.Sensor == "cpu" && Math.Abs(t.TempCelsius - 46.125f) < 0.01f);
        Assert.Contains(result.Temperatures, t => t.Sensor == "gpu" && t.TempCelsius == 43f);

        // Assert - Power Rails
        Assert.Equal(3, result.PowerRails.Length);
        Assert.Contains(result.PowerRails, p => p.Name == "VDD_IN" && p.CurrentMilliwatts == 6120);
        Assert.Contains(result.PowerRails, p => p.Name == "VDD_CPU_GPU_CV" && p.CurrentMilliwatts == 436);
        Assert.Contains(result.PowerRails, p => p.Name == "VDD_SOC" && p.CurrentMilliwatts == 2464);
    }

    [Fact]
    public void TegraStatsLine_TryParse_ValidLine_ReturnsTrue()
    {
        // Act
        bool success = TegraStatsLine.TryParse(SampleTegraStatsLine, null, out var result);

        // Assert
        Assert.True(success);
        Assert.Equal(1729, result.Ram.UsedMB);
        Assert.Equal(0, result.GpuUtilizationPercent);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid line")]
    [InlineData("11-23-2025 22:47:56")]
    [InlineData("RAM 1729/15657MB")]
    public void TegraStatsLine_TryParse_InvalidLine_ReturnsFalse(string input)
    {
        // Act
        bool success = TegraStatsLine.TryParse(input, null, out var result);

        // Assert
        Assert.False(success);
        Assert.Equal(default, result);
    }

    [Fact]
    public void TegraStatsLine_Parse_InvalidLine_ThrowsFormatException()
    {
        // Arrange
        string invalidLine = "completely invalid tegrastats line";

        // Act & Assert
        Assert.Throws<FormatException>(() => TegraStatsLine.Parse(invalidLine));
    }

    [Fact]
    public void TegraStatsLine_GetTemperature_ExistingSensor_ReturnsTemperature()
    {
        // Arrange
        var stats = TegraStatsLine.Parse(SampleTegraStatsLine);

        // Act
        var cpuTemp = stats.GetTemperature("cpu");
        var gpuTemp = stats.GetTemperature("gpu");

        // Assert
        Assert.NotNull(cpuTemp);
        Assert.InRange(cpuTemp.Value, 46.1f, 46.2f);
        Assert.NotNull(gpuTemp);
        Assert.Equal(43f, gpuTemp.Value);
    }

    [Fact]
    public void TegraStatsLine_GetTemperature_NonExistentSensor_ReturnsNull()
    {
        // Arrange
        var stats = TegraStatsLine.Parse(SampleTegraStatsLine);

        // Act
        var temp = stats.GetTemperature("nonexistent");

        // Assert
        Assert.Null(temp);
    }

    [Fact]
    public void TegraStatsLine_GetTemperature_CaseInsensitive_ReturnsTemperature()
    {
        // Arrange
        var stats = TegraStatsLine.Parse(SampleTegraStatsLine);

        // Act
        var temp1 = stats.GetTemperature("CPU");
        var temp2 = stats.GetTemperature("Cpu");
        var temp3 = stats.GetTemperature("cpu");

        // Assert
        Assert.NotNull(temp1);
        Assert.NotNull(temp2);
        Assert.NotNull(temp3);
        Assert.Equal(temp1, temp2);
        Assert.Equal(temp2, temp3);
    }

    [Fact]
    public void TegraStatsLine_GetPowerRail_ExistingRail_ReturnsPowerReading()
    {
        // Arrange
        var stats = TegraStatsLine.Parse(SampleTegraStatsLine);

        // Act
        var vddIn = stats.GetPowerRail("VDD_IN");
        var vddSoc = stats.GetPowerRail("VDD_SOC");

        // Assert
        Assert.NotNull(vddIn);
        Assert.Equal(6120, vddIn.Value.CurrentMilliwatts);
        Assert.NotNull(vddSoc);
        Assert.Equal(2464, vddSoc.Value.CurrentMilliwatts);
    }

    [Fact]
    public void TegraStatsLine_GetPowerRail_NonExistentRail_ReturnsNull()
    {
        // Arrange
        var stats = TegraStatsLine.Parse(SampleTegraStatsLine);

        // Act
        var power = stats.GetPowerRail("VDD_NONEXISTENT");

        // Assert
        Assert.Null(power);
    }

    [Fact]
    public void TegraStatsLine_TotalPowerMilliwatts_SumsAllRails()
    {
        // Arrange
        var stats = TegraStatsLine.Parse(SampleTegraStatsLine);

        // Act
        var totalPower = stats.TotalPowerMilliwatts;

        // Assert
        // VDD_IN (6120) + VDD_CPU_GPU_CV (436) + VDD_SOC (2464) = 9020
        Assert.Equal(9020, totalPower);
    }

    [Fact]
    public void TegraStatsLine_Parse_DifferentGpuUtilization_ParsesCorrectly()
    {
        // Arrange
        string lineWithGpu = SampleTegraStatsLine.Replace("GR3D_FREQ 0%", "GR3D_FREQ 85%");

        // Act
        var result = TegraStatsLine.Parse(lineWithGpu);

        // Assert
        Assert.Equal(85, result.GpuUtilizationPercent);
    }

    [Fact]
    public void TegraStatsLine_Parse_AllCpuCoresOnline_ParsesCorrectly()
    {
        // Arrange
        string lineWithAllOnline = SampleTegraStatsLine.Replace(
            "CPU [1%@1420,2%@1420,4%@1420,3%@1420,off,off,off,off]",
            "CPU [10%@2000,20%@2000,30%@2000,40%@2000,50%@2000,60%@2000,70%@2000,80%@2000]");

        // Act
        var result = TegraStatsLine.Parse(lineWithAllOnline);

        // Assert
        Assert.Equal(8, result.Cpu.OnlineCoreCount);
        Assert.All(result.Cpu.Cores, core => Assert.True(core.IsOnline));
    }

    [Fact]
    public void TegraStatsLine_Parse_HighSwapUsage_ParsesCorrectly()
    {
        // Arrange
        string lineWithSwap = SampleTegraStatsLine.Replace(
            "SWAP 0/7828MB (cached 0MB)",
            "SWAP 4096/7828MB (cached 1024MB)");

        // Act
        var result = TegraStatsLine.Parse(lineWithSwap);

        // Assert
        Assert.Equal(4096, result.Swap.UsedMB);
        Assert.Equal(1024, result.Swap.CachedMB);
        Assert.InRange(result.Swap.UsagePercent, 52f, 53f);
    }

    [Fact]
    public void TegraStatsLine_Parse_VariablePowerReadings_ParsesCorrectly()
    {
        // Arrange
        string lineWithDiffPower = SampleTegraStatsLine.Replace(
            "VDD_IN 6120mW/6120mW",
            "VDD_IN 5000mW/6000mW");

        // Act
        var result = TegraStatsLine.Parse(lineWithDiffPower);

        // Assert
        var vddIn = result.GetPowerRail("VDD_IN");
        Assert.NotNull(vddIn);
        Assert.Equal(5000, vddIn.Value.CurrentMilliwatts);
        Assert.Equal(6000, vddIn.Value.AverageMilliwatts);
    }

    #endregion

    #region Real-World Sample Tests

    [Theory]
    [InlineData("11-23-2025 22:47:56 RAM 1729/15657MB (lfb 8x4MB) SWAP 0/7828MB (cached 0MB) CPU [1%@1420,2%@1420,4%@1420,3%@1420,off,off,off,off] GR3D_FREQ 0% cv0@43.562C cpu@46.125C soc2@43.781C soc0@44.656C cv1@43.312C gpu@43C tj@46.937C soc1@46.937C cv2@43.781C VDD_IN 6120mW/6120mW VDD_CPU_GPU_CV 436mW/436mW VDD_SOC 2464mW/2464mW")]
    [InlineData("11-23-2025 22:48:37 RAM 1733/15657MB (lfb 8x4MB) SWAP 0/7828MB (cached 0MB) CPU [1%@1420,0%@1420,0%@1420,0%@1420,off,off,off,off] GR3D_FREQ 0% cv0@43.437C cpu@46.062C soc2@43.781C soc0@44.718C cv1@43.312C gpu@43.343C tj@46.812C soc1@46.812C cv2@43.781C VDD_IN 6090mW/6105mW VDD_CPU_GPU_CV 396mW/416mW VDD_SOC 2464mW/2464mW")]
    [InlineData("11-23-2025 22:48:42 RAM 1733/15657MB (lfb 8x4MB) SWAP 0/7828MB (cached 0MB) CPU [1%@729,2%@729,1%@729,1%@729,off,off,off,off] GR3D_FREQ 0% cv0@43.406C cpu@46.156C soc2@43.812C soc0@44.718C cv1@43.468C gpu@43.125C tj@47C soc1@47C cv2@43.812C VDD_IN 6090mW/6094mW VDD_CPU_GPU_CV 396mW/401mW VDD_SOC 2464mW/2464mW")]
    public void TegraStatsLine_Parse_RealWorldSamples_ParsesSuccessfully(string realWorldLine)
    {
        // Act
        bool success = TegraStatsLine.TryParse(realWorldLine, null, out var result);

        // Assert
        Assert.True(success);
        Assert.True(result.Timestamp.Year == 2025);
        Assert.InRange(result.Ram.UsedMB, 0, result.Ram.TotalMB);
        Assert.InRange(result.Swap.UsedMB, 0, result.Swap.TotalMB);
        Assert.InRange(result.GpuUtilizationPercent, 0, 100);
        Assert.NotEmpty(result.Temperatures);
        Assert.NotEmpty(result.PowerRails);
    }

    #endregion

    #region Edge Cases and Boundary Tests

    [Fact]
    public void RamInfo_ZeroUsage_ParsesCorrectly()
    {
        // Arrange
        string input = "0/16000MB (lfb 16x4MB)";

        // Act
        var result = RamInfo.Parse(input);

        // Assert
        Assert.Equal(0, result.UsedMB);
        Assert.Equal(0f, result.UsagePercent);
    }

    [Fact]
    public void RamInfo_FullUsage_ParsesCorrectly()
    {
        // Arrange
        string input = "16000/16000MB (lfb 0x4MB)";

        // Act
        var result = RamInfo.Parse(input);

        // Assert
        Assert.Equal(16000, result.UsedMB);
        Assert.Equal(100f, result.UsagePercent);
    }

    [Fact]
    public void CpuInfo_SingleCore_ParsesCorrectly()
    {
        // Arrange
        string input = "[50%@1500]";

        // Act
        var result = CpuInfo.Parse(input);

        // Assert
        Assert.Single(result.Cores);
        Assert.Equal(1, result.OnlineCoreCount);
        Assert.Equal(50f, result.AverageUtilization);
    }

    [Fact]
    public void TegraStatsLine_Parse_ZeroGpuUtilization_ParsesCorrectly()
    {
        // Arrange
        var stats = TegraStatsLine.Parse(SampleTegraStatsLine);

        // Act & Assert
        Assert.Equal(0, stats.GpuUtilizationPercent);
    }

    [Fact]
    public void TemperatureReading_IntegerTemperature_ParsesCorrectly()
    {
        // Arrange
        string input = "gpu@43C";

        // Act
        var result = TemperatureReading.Parse(input);

        // Assert
        Assert.Equal("gpu", result.Sensor);
        Assert.Equal(43f, result.TempCelsius);
    }

    #endregion
}
