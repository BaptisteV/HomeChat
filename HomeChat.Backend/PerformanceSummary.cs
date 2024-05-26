using NvAPIWrapper.GPU;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace HomeChat.Backend;


public class PerformanceMonitor : IPerformanceMonitor
{
    private readonly ILogger<PerformanceMonitor> _logger;

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;

        public MEMORYSTATUSEX()
        {
            dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            dwMemoryLoad = 0;
            ullTotalPhys = 0;
            ullAvailPhys = 0;
            ullTotalPageFile = 0;
            ullAvailPageFile = 0;
            ullTotalVirtual = 0;
            ullAvailVirtual = 0;
            ullAvailExtendedVirtual = 0;
        }
    }

    public PerformanceMonitor(ILogger<PerformanceMonitor> logger)
    {
        _logger = logger;
    }

    public async Task<PerformanceSummary> GetPerformanceSummaryAsync()
    {
        try
        {
            var cpuUsage = await GetCpuUsageAsync();
            var ramUsage = GetRamUsage();
            var gpuUsage = GetGpuUsage();
            return new PerformanceSummary(cpuUsage, gpuUsage, ramUsage);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError($"Failed to get CPU usage: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError($"Failed to get RAM usage: {ex.Message}");
        }
        catch (NvAPIWrapperException ex)
        {
            _logger.LogError($"Failed to get GPU usage: {ex.Message}");
        }

        return new PerformanceSummary(0, 0, 0);
    }

    private async Task<int> GetCpuUsageAsync()
    {
        var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        cpuCounter.NextValue(); // The first call will always return 0
        await Task.Delay(1000); // Wait a second to get a valid reading
        return (int)cpuCounter.NextValue();
    }

    private int GetRamUsage()
    {
        var memStatus = new MEMORYSTATUSEX();
        if (!GlobalMemoryStatusEx(ref memStatus))
        {
            throw new UnauthorizedAccessException("Failed to get RAM usage.");
        }

        var availableRAM = memStatus.ullAvailPhys / (1024 * 1024);
        var totalRAM = memStatus.ullTotalPhys / (1024 * 1024);
        var ramUsage = 100 - ((double)availableRAM / totalRAM * 100);
        return (int)ramUsage;
    }

    private int GetGpuUsage()
    {
        var gpus = PhysicalGPU.GetPhysicalGPUs();
        if (gpus.Length == 0)
        {
            _logger.LogWarning("No GPUs found.");
            return 0;
        }
        else if (gpus.Length > 1)
        {
            _logger.LogWarning($"Multiple GPUs found. Using the first GPU.");
        }

        var gpu = gpus[0]; // Assuming we are only interested in the first GPU
        return gpu.UsageInformation.GPU.Percentage;
    }
}
public record PerformanceSummary(int Cpu, int Gpu, int Ram);
