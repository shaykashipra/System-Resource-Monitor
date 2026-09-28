namespace SystemResourceMonitor.Models;

public sealed class ProcessInfo(string name, int id, double memoryMegabytes, int threadCount, string status)
{
    public string Name { get; } = name;
    public int Id { get; } = id;
    public double MemoryMegabytes { get; } = memoryMegabytes;
    public string MemoryDisplay => $"{MemoryMegabytes:N1} MB";
    public int ThreadCount { get; } = threadCount;
    public string Status { get; } = status;
}
