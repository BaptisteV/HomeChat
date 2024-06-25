using HomeChat.Backend.Chats;
using HomeChat.Backend.Performances.Exceptions;
using NvAPIWrapper.GPU;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace HomeChat.Backend.Performances;


public partial class PerformanceMonitor : IPerformanceMonitor
{
    private readonly ILogger<PerformanceMonitor> _logger;
    private readonly IChatSessionManager _chatSessionManager;

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

    public PerformanceMonitor(ILogger<PerformanceMonitor> logger, IChatSessionManager chatSessionManager)
    {
        _logger = logger;
        _chatSessionManager = chatSessionManager;
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
        var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        return new CpuUsage() { PercentUsed = (int)cpuCounter.NextValue() };
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
        var gpus = PhysicalGPU.GetPhysicalGPUs();
        if (gpus.Length == 0)
        {
            _logger.LogWarning("No GPUs found.");
            return new GpuUsage();
        }
        else if (gpus.Length > 1)
        {
            _logger.LogWarning("Multiple GPUs found. Using the first GPU.");
        }

        var gpu = gpus[0]; // Assuming we are only interested in the first GPU
        return new GpuUsage() { PercentUsed = gpu.UsageInformation.GPU.Percentage };
    }

    private async Task DeleteIncativeSessionsUntil(Func<bool> until)
    {
        const int MAX_RETRIES = 10;
        int retries = 0;
        do
        {
            await DeleteInactiveSessions();
            retries++;
        }
        while (until() && retries <= MAX_RETRIES);
    }

    public async Task DeleteSessionForRam(long freeRamTargetInMb)
    {
        await DeleteIncativeSessionsUntil(() =>
        {
            var ramUsage = GetRamUsage();
            return ramUsage.Free > freeRamTargetInMb;
        });
    }

    public List<SessionInfo> InactiveSessions(IEnumerable<SessionInfo> sessions)
    {
        var biggest = sessions
            .OrderBy(s => s.Model.SizeInMb)
            .Take(Math.Max(sessions.Count() / 4, 1));
        return biggest
            .OrderBy(s => s.LastActivity)
            .Take(Math.Max(biggest.Count() / 4, 1))
            .ToList();
    }


    public async Task DeleteMostInactiveSession()
    {
        var sessions = await _chatSessionManager.GetSessions();
        if (sessions.Count() <= 1)
        {
            return;
        }

        var mostInactiveSession = InactiveSessions(sessions)[0];
        await _chatSessionManager.DeleteSession(mostInactiveSession.Id);
    }

    public async Task DeleteInactiveSessions()
    {
        var sessions = await _chatSessionManager.GetSessions();
        if (sessions.Count() <= 1)
        {
            return;
        }

        var inactiveSessions = InactiveSessions(sessions);

        var deleteTasks = inactiveSessions.Select(s => _chatSessionManager.DeleteSession(s.Id));
        await Task.WhenAll(deleteTasks);
        _logger.LogInformation("{InactiveSessionsCount} sessions deleted", inactiveSessions.Count);
    }
}
