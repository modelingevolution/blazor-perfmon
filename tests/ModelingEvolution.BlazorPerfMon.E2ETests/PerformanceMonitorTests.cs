using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace ModelingEvolution.BlazorPerfMon.E2ETests;

public class PerformanceMonitorTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private readonly List<string> _consoleMessages = new();
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage? _page;

    public PerformanceMonitorTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        _consoleMessages.Clear();

        // Initialize Playwright
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        _page = await _browser.NewPageAsync();

        // Capture all console messages from the browser
        _page.Console += (_, msg) =>
        {
            var message = $"[{msg.Type}] {msg.Text}";
            _consoleMessages.Add(message);
            _output.WriteLine(message); // Output to test console
        };

        // Navigate to the application
        await _page.GotoAsync("http://localhost:5062");
    }

    public async Task DisposeAsync()
    {
        if (_page != null) await _page.CloseAsync();
        if (_browser != null) await _browser.CloseAsync();
        _playwright?.Dispose();
    }

    [Fact]
    public async Task ApplicationLoadsSuccessfully()
    {
        Assert.NotNull(_page);

        // Wait for the canvas to be present
        var canvas = _page!.Locator("canvas");
        await canvas.WaitForAsync(new() { Timeout = 10000, State = WaitForSelectorState.Visible });

        // Wait a bit for some data to be rendered
        await _page.WaitForTimeoutAsync(5000);

        // Verify we got some console output (debug logs from interpolation)
        _output.WriteLine($"Total console messages captured: {_consoleMessages.Count}");

        // Check for interpolation logs (only present in Debug builds)
        var interpolationLogs = _consoleMessages
            .Where(m => m.Contains("[TimeSeriesChart]"))
            .ToList();

        if (interpolationLogs.Any())
        {
            _output.WriteLine($"Interpolation logs found: {interpolationLogs.Count}");
            foreach (var log in interpolationLogs.Take(10))
            {
                _output.WriteLine(log);
            }
        }
        else
        {
            _output.WriteLine("No interpolation logs found (might be Release build)");
        }
    }

    [Fact]
    public async Task StatusIndicatorChangesColor()
    {
        Assert.NotNull(_page);

        // Wait for the status circle to be present
        var statusCircle = _page!.Locator(".status-circle");
        await statusCircle.WaitForAsync(new() { Timeout = 10000, State = WaitForSelectorState.Visible });

        // Wait for connection to establish (should turn green or yellow)
        await _page.WaitForTimeoutAsync(3000);

        // Get the class attribute
        var className = await statusCircle.GetAttributeAsync("class");
        _output.WriteLine($"Status circle class: {className}");

        // Should not be red (disconnected) after connection
        Assert.DoesNotContain("status-red", className);
    }

    [Fact]
    public async Task ChartsRenderWithData()
    {
        Assert.NotNull(_page);

        // Wait for canvas
        var canvas = _page!.Locator("canvas");
        await canvas.WaitForAsync(new() { Timeout = 10000, State = WaitForSelectorState.Visible });

        // Wait for data to arrive and render
        await _page.WaitForTimeoutAsync(5000);

        // Take a screenshot for visual verification
        await _page.ScreenshotAsync(new()
        {
            Path = "test-output-charts.png",
            FullPage = true
        });

        _output.WriteLine("Screenshot saved to test-output-charts.png");

        // Verify console doesn't have errors
        var errors = _consoleMessages.Where(m => m.Contains("[error]")).ToList();
        Assert.Empty(errors);
    }
}
