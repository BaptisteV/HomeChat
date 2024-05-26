using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HomeChat.PerfExemple;

public class PerfSample
{
    public float CpuPercentage { get; set; }
    public int RamAvailableInMb { get; set; }
    public int RamCommitedInMb { get; set; }
    public List<double> CpuHistory { get; set; } = new();
    public static int CpuHistoryMax = 25;
}

public interface IPerfMonitor
{
    PerfSample GetPerfSample();
    IAsyncEnumerable<PerfSample> GetPerfSamples();
}

public class PerfMonitor : IPerfMonitor, IDisposable
{
    public TimeSpan Interval
    {
        get
        {
            return _interval;
        }

        set
        {
            _interval = value;
            _timer.Period = value;
        }
    }
    private TimeSpan _interval = TimeSpan.FromMilliseconds(250);
    readonly PeriodicTimer _timer;

    private readonly PerformanceCounter _cpuCounter;
    private readonly PerformanceCounter _ramAvailableCounter;
    private readonly PerformanceCounter _ramCommitedCounter;

    private List<double> _cpuHistory;
    private Queue<double> _cpuHistoryQ;

    public PerfMonitor()
    {
        _timer = new PeriodicTimer(Interval);
        _cpuHistory = new List<double>();
        _cpuHistoryQ = new Queue<double>();

        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        _ramAvailableCounter = new PerformanceCounter("Memory", "Available Bytes");
        _ramCommitedCounter = new PerformanceCounter("Memory", "Committed Bytes");
        // First is always 0%
        _cpuCounter.NextValue();
        _ramAvailableCounter.NextValue();
        _ramCommitedCounter.NextValue();
    }

    public PerfSample GetPerfSample()
    {
        var ramAvailable = (long)Math.Round(_ramAvailableCounter.NextValue());
        var ramCommited = (long)Math.Round(_ramCommitedCounter.NextValue());
        var sample = new PerfSample()
        {
            CpuPercentage = _cpuCounter.NextValue(),
            RamAvailableInMb = (int)(ramAvailable / 1_048_576),
            RamCommitedInMb = (int)(ramCommited / 1_048_576),
        };

        Historize(sample);

        return sample;
    }

    private void Historize(PerfSample sample)
    {
        _cpuHistoryQ.Enqueue(sample.CpuPercentage);
        if (_cpuHistoryQ.Count > PerfSample.CpuHistoryMax)
        {
            _cpuHistoryQ.Dequeue();
        }

        if (_cpuHistory.Count > PerfSample.CpuHistoryMax)
        {
            _cpuHistory.RemoveAt(_cpuHistory.Count - 1);
        }
        _cpuHistory.Add(sample.CpuPercentage);
        sample.CpuHistory = new List<double>(_cpuHistory);
        sample.CpuHistory = new List<double>(_cpuHistoryQ.ToList());
    }

    public async IAsyncEnumerable<PerfSample> GetPerfSamples()
    {
        while (true)
        {
            yield return GetPerfSample();
            await _timer.WaitForNextTickAsync();
        }
    }

    public void Dispose()
    {
        _cpuCounter.Dispose();
        _ramAvailableCounter.Dispose();
        _ramCommitedCounter.Dispose();
    }
}
