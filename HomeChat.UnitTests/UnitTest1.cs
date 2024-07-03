using HomeChat.Backend.Performances;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using Xunit.Abstractions;
using Xunit.Microsoft.DependencyInjection;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace HomeChat.UnitTests;

public class UnitTest1 : TestBedFixture
{
    private readonly IPerformanceMonitor _perfMonitor;
    private readonly ITestOutputHelper _outputHelper;

    public UnitTest1(ITestOutputHelper output)
    {
        _outputHelper = output;
        _perfMonitor = GetService<IPerformanceMonitor>(output);
    }

    [Fact]
    public void SinglePerformanceSummary()
    {
        var summary = _perfMonitor.GetPerformanceSummary();

        Assert.InRange(summary.Cpu.PercentUsed, 0, 100);
        Assert.InRange(summary.Gpu.PercentUsed, 0, 100);
        Assert.InRange(summary.Ram.PercentUsed, 1, 99);
        Assert.True(summary.Ram.Free > 0);
        Assert.True(summary.Ram.Available > 0);
    }

    [Theory]
    [InlineData(1000)]
    public void Sequential(int nIteration)
    {
        for (int i = 0; i < nIteration; i++)
        {
            var summary = _perfMonitor.GetPerformanceSummary();

            Assert.InRange(summary.Cpu.PercentUsed, 0, 100);
            Assert.InRange(summary.Gpu.PercentUsed, 0, 100);
            Assert.InRange(summary.Ram.PercentUsed, 1, 99);
            Assert.True(summary.Ram.Free > 0);
            Assert.True(summary.Ram.Available > 0);
        }
    }

    [Theory]
    [InlineData(1000)]
    public async Task WhenAll(int nIteration)
    {
        var tasks = new List<Task<PerformanceSummary>>();
        for (int i = 0; i < nIteration; i++)
        {
            tasks.Add(Task.Run(_perfMonitor.GetPerformanceSummary));
        }
        await Task.WhenAll(tasks);

        foreach (var summary in tasks.Select(t => t.Result))
        {
            Assert.InRange(summary.Cpu.PercentUsed, 0, 100);
            Assert.InRange(summary.Gpu.PercentUsed, 0, 100);
            Assert.InRange(summary.Ram.PercentUsed, 1, 99);
            Assert.True(summary.Ram.Free > 0);
            Assert.True(summary.Ram.Available > 0);
        }
    }

    [Theory]
    [InlineData(1000)]
    public void Threaded(int nIteration)
    {
        var tasks = new List<Thread>();
        var summaries = new ConcurrentBag<PerformanceSummary>();
        for (int i = 0; i < nIteration; i++)
        {
            var thread = new Thread(() => summaries.Add(_perfMonitor.GetPerformanceSummary()));
            thread.Start();
            tasks.Add(thread);
        }

        foreach (var thread in tasks)
        {
            thread.Join();
        }

        foreach (var summary in summaries)
        {
            Assert.InRange(summary.Cpu.PercentUsed, 0, 100);
            Assert.InRange(summary.Gpu.PercentUsed, 0, 100);
            Assert.InRange(summary.Ram.PercentUsed, 1, 99);
            Assert.True(summary.Ram.Free > 0);
            Assert.True(summary.Ram.Available > 0);
        }
    }

    protected override void AddServices(IServiceCollection services, IConfiguration? configuration)
    {
        services.AddSingleton<IPerformanceMonitor, PerformanceMonitor>();
    }

    protected override IEnumerable<TestAppSettings> GetTestAppSettings()
    {
        return Enumerable.Empty<TestAppSettings>();
    }

    protected override ValueTask DisposeAsyncCore()
    {
        return ValueTask.CompletedTask;
    }
}