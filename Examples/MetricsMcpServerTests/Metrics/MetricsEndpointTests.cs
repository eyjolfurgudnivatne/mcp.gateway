namespace MetricsMcpServerTests.Metrics;

using MetricsMcpServerTests.Fixture;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

[Collection("ServerCollection")]
public class MetricsEndpointTests(MetricsMcpServerFixture fixture)
{
    [Fact]
    public async Task Metrics_AfterToolInvocation_TracksInvocationCount()
    {
        // Arrange - Call add tool
        var addRequest = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            id = "1",
            @params = new
            {
                name = "add",
                arguments = new { a = 5.0, b = 3.0 }
            }
        };

        await fixture.HttpClient.PostAsJsonAsync("/rpc", addRequest, fixture.CancellationToken);

        // Act - Get metrics
        var metricsResponse = await fixture.HttpClient.GetAsync("/metrics", fixture.CancellationToken);
        metricsResponse.EnsureSuccessStatusCode();

        var content = await metricsResponse.Content.ReadAsStringAsync(fixture.CancellationToken);
        var metrics = JsonDocument.Parse(content).RootElement;

        // Assert
        Assert.True(metrics.TryGetProperty("metrics", out var metricsArray));
        var addMetrics = metricsArray.EnumerateArray()
            .FirstOrDefault(m => m.GetProperty("tool").GetString() == "add");

        Assert.NotEqual(default, addMetrics);
        Assert.True(addMetrics.GetProperty("invocations").GetInt64() >= 1);
        Assert.True(addMetrics.GetProperty("successes").GetInt64() >= 1);
        Assert.Equal(0, addMetrics.GetProperty("failures").GetInt64());
    }

    [Fact]
    public async Task Metrics_AfterFailedInvocation_TracksFailures()
    {
        // Arrange - Call divide by zero
        var divideRequest = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            id = "2",
            @params = new
            {
                name = "divide",
                arguments = new { a = 10.0, b = 0.0 }
            }
        };

        await fixture.HttpClient.PostAsJsonAsync("/rpc", divideRequest, fixture.CancellationToken);

        // Act - Get metrics
        var metricsResponse = await fixture.HttpClient.GetAsync("/metrics", fixture.CancellationToken);
        metricsResponse.EnsureSuccessStatusCode();

        var content = await metricsResponse.Content.ReadAsStringAsync(fixture.CancellationToken);
        var metrics = JsonDocument.Parse(content).RootElement;

        // Assert
        Assert.True(metrics.TryGetProperty("metrics", out var metricsArray));
        var divideMetrics = metricsArray.EnumerateArray()
            .FirstOrDefault(m => m.GetProperty("tool").GetString() == "divide");

        Assert.NotEqual(default, divideMetrics);
        Assert.True(divideMetrics.GetProperty("failures").GetInt64() >= 1);
        
        // Check error tracking - NOTE: Exception is wrapped in TargetInvocationException
        // because InvokeFunctionDelegate uses DynamicInvoke
        var errors = divideMetrics.GetProperty("errors");
        Assert.True(errors.TryGetProperty("TargetInvocationException", out _), 
            $"Expected TargetInvocationException (wraps ToolInvalidParamsException), got: {errors.GetRawText()}");
    }

    [Fact]
    public async Task Metrics_TracksSuccessRate()
    {
        // Arrange - Call add twice (success)
        var addRequest = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            id = "3",
            @params = new
            {
                name = "add",
                arguments = new { a = 1.0, b = 2.0 }
            }
        };

        await fixture.HttpClient.PostAsJsonAsync("/rpc", addRequest, fixture.CancellationToken);
        await fixture.HttpClient.PostAsJsonAsync("/rpc", addRequest, fixture.CancellationToken);

        // Act - Get metrics
        var metricsResponse = await fixture.HttpClient.GetAsync("/metrics", fixture.CancellationToken);
        metricsResponse.EnsureSuccessStatusCode();

        var content = await metricsResponse.Content.ReadAsStringAsync(fixture.CancellationToken);
        var metrics = JsonDocument.Parse(content).RootElement;

        // Assert
        Assert.True(metrics.TryGetProperty("metrics", out var metricsArray));
        var addMetrics = metricsArray.EnumerateArray()
            .FirstOrDefault(m => m.GetProperty("tool").GetString() == "add");

        Assert.NotEqual(default, addMetrics);
        var successRate = addMetrics.GetProperty("successRate").GetDouble();
        Assert.True(successRate >= 0.0 && successRate <= 100.0);
    }

    [Fact]
    public async Task Metrics_TracksDuration()
    {
        // Arrange - Call slow operation
        var slowRequest = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            id = "4",
            @params = new
            {
                name = "slow_operation",
                arguments = new { delayMs = 50 }
            }
        };

        await fixture.HttpClient.PostAsJsonAsync("/rpc", slowRequest, fixture.CancellationToken);

        // Act - Get metrics
        var metricsResponse = await fixture.HttpClient.GetAsync("/metrics", fixture.CancellationToken);
        metricsResponse.EnsureSuccessStatusCode();

        var content = await metricsResponse.Content.ReadAsStringAsync(fixture.CancellationToken);
        var metrics = JsonDocument.Parse(content).RootElement;

        // Assert
        Assert.True(metrics.TryGetProperty("metrics", out var metricsArray));
        var slowMetrics = metricsArray.EnumerateArray()
            .FirstOrDefault(m => m.GetProperty("tool").GetString() == "slow_operation");

        Assert.NotEqual(default, slowMetrics);
        Assert.True(slowMetrics.GetProperty("avgDuration").GetDouble() >= 50.0);
        Assert.True(slowMetrics.GetProperty("minDuration").GetDouble() >= 0.0);
        Assert.True(slowMetrics.GetProperty("maxDuration").GetDouble() >= 50.0);
    }
}
