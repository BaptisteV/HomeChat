using HomeChat.Backend.Chats;
using HomeChat.Backend.Performances.Exceptions;
using NvAPIWrapper.GPU;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace HomeChat.Backend.Performances;


public class PerformanceMonitor : IPerformanceMonitor
{
    private readonly ILogger<PerformanceMonitor> _logger;
    private readonly IChatSessionManager _chatSessionManager;

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);

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

    public PerformanceMonitor(ILogger<PerformanceMonitor> logger, IChatSessionManager chatSessionManager)
    {
        _logger = logger;
        _chatSessionManager = chatSessionManager;
    }

    public async Task<PerformanceSummary> GetPerformanceSummaryAsync()
    {
        int cpuUsage = 0, ramUsage = 0, gpuUsage = 0;

        try
        {
            cpuUsage = await GetCpuUsageAsync();
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

    private async Task<int> GetCpuUsageAsync()
    {
        var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        cpuCounter.NextValue(); // The first call will always return 0
        await Task.Delay(1000); // Wait a second to get a valid reading
        return (int)cpuCounter.NextValue();
    }

    private int GetRamUsage()
    {
        var memStatus = new MemoryStatusEx();
        if (!GlobalMemoryStatusEx(ref memStatus))
        {
            throw new UnauthorizedAccessException("Failed to get RAM usage.");
        }

        var availableRAM = memStatus.ullAvailPhys / (1024 * 1024);
        var totalRAM = memStatus.ullTotalPhys / (1024 * 1024);
        var ramUsage = 100 - (double)availableRAM / totalRAM * 100;
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
            _logger.LogWarning("Multiple GPUs found. Using the first GPU.");
        }

        var gpu = gpus[0]; // Assuming we are only interested in the first GPU
        return gpu.UsageInformation.GPU.Percentage;
    }

    public async Task DeleteInactiveSessions()
    {
        var sessions = await _chatSessionManager.GetSessions();
        if (sessions.Count() <= 1)
        {
            return;
        }

        var inactiveSessions = sessions
            .OrderBy(s => s.LastActivity)
            .Take(sessions.Count() / 4)
            .OrderBy(s => s.Model.SizeInMb)
            .Take(sessions.Count() / 4)
            .ToList();

        var deleteTasks = inactiveSessions.Select(s => _chatSessionManager.DeleteSession(s.Id));
        await Task.WhenAll(deleteTasks);
        _logger.LogInformation("{InactiveSessionsCount} sessions deleted", inactiveSessions.Count);
    }
}

public record PerformanceSummary(int Cpu, int Gpu, int Ram);
