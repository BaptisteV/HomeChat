namespace HomeChat.Backend.Performances;

public record Usage
{
    public int PercentUsed { get; set; }
    public int Available { get; set; }
    public int Free { get; set; }
}

public record RamUsagee : Usage, IUsage;
public record CpuUsage : Usage, IUsage;
public record GpuUsage : Usage, IUsage;

public record PerformanceSummary(CpuUsage Cpu, GpuUsage Gpu, RamUsagee Ram);
