using HomeChat.Backend.Performances.Exceptions;
using NvAPIWrapper.GPU;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace HomeChat.Backend.Performances;


public partial class PerformanceMonitor : IPerformanceMonitor
{
    private readonly ILogger<PerformanceMonitor> _logger;
    private readonly PerformanceCounter _cpuCounter;
    private readonly PhysicalGPU _gpu;

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MemoryStatusEx
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

        public MemoryStatusEx()
        {
            dwLength = (uint)Marshal.SizeOf(typeof(MemoryStatusEx));
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
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        var gpus = PhysicalGPU.GetPhysicalGPUs();
        if (gpus.Length == 0)
        {
            _logger.LogWarning("No GPUs found.");
        }
        else if (gpus.Length > 1)
        {
            _logger.LogWarning("Multiple GPUs found. Using the first GPU.");
        }
        _gpu = gpus[0];
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
    }

    public PerformanceSummary GetPerformanceSummary()
    {
        var cpuUsage = new CpuUsage();
        var ramUsage = new RamUsagee();
        var gpuUsage = new GpuUsage();
        try
        {
            cpuUsage = GetCpuUsage();
        }
        catch (CpuUsageException ex)
        {
            _logger.LogError(ex, "Failed to get CPU usage");
        }

        try
        {
            ramUsage = GetRamUsage();
        }
        catch (RamUsageException ex)
        {
            _logger.LogError(ex, "Failed to get RAM usage");
        }

        try
        {
            gpuUsage = GetGpuUsage();
        }
        catch (GpuUsageException ex)
        {
            _logger.LogError(ex, "Failed to get GPU usage");
        }

        return new PerformanceSummary(cpuUsage, gpuUsage, ramUsage);
    }

    private CpuUsage GetCpuUsage()
    {
        var percent = _cpuCounter.NextValue();
        int maxTries = 10;
        while (percent <= 0 || percent >= 100 && maxTries > 0)
        {
            percent = _cpuCounter.NextValue();
            maxTries--;
        }

        return new CpuUsage() { PercentUsed = (int)percent };
    }

    private RamUsagee GetRamUsage()
    {
        var memStatus = new MemoryStatusEx();
        if (!GlobalMemoryStatusEx(ref memStatus))
        {
            throw new UnauthorizedAccessException("Failed to get RAM usage.");
        }

        var availableRam = memStatus.ullAvailPhys / (1024 * 1024);
        var totalRAM = memStatus.ullTotalPhys / (1024 * 1024);
        var ramUsage = 100 - (double)availableRam / totalRAM * 100;
        return new RamUsagee() { PercentUsed = (int)ramUsage, Available = (int)availableRam, Free = (int)totalRAM - (int)availableRam };
    }

    private GpuUsage GetGpuUsage()
    {
        return new GpuUsage() { PercentUsed = _gpu.UsageInformation.GPU.Percentage };
    }
}
