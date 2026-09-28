namespace SystemResourceMonitor.Models;

public sealed record MonitoringSnapshot(
    double CpuPercent,
    double MemoryPercent,
    string MemoryDetail,
    double DiskPercent,
    string DiskDetail,
    IReadOnlyList<ProcessInfo> Processes,
    DateTime CollectedAt);
