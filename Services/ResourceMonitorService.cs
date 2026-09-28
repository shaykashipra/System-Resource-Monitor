using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using SystemResourceMonitor.Models;

namespace SystemResourceMonitor.Services;

public sealed class ResourceMonitorService
{
    private FileTime _previousIdle;
    private FileTime _previousKernel;
    private FileTime _previousUser;
    private bool _hasCpuSample;

    public MonitoringSnapshot GetSnapshot()
    {
        var memory = GetMemoryStatus();
        var disk = GetPrimaryDiskUsage();
        return new MonitoringSnapshot(
            GetCpuUsagePercent(),
            memory.percent,
            memory.detail,
            disk.percent,
            disk.detail,
            GetProcesses(),
            DateTime.Now);
    }

    private double GetCpuUsagePercent()
    {
        if (!GetSystemTimes(out var idle, out var kernel, out var user)) return 0;
        if (!_hasCpuSample)
        {
            _previousIdle = idle;
            _previousKernel = kernel;
            _previousUser = user;
            _hasCpuSample = true;
            return 0;
        }

        ulong idleDelta = ToUInt64(idle) - ToUInt64(_previousIdle);
        ulong kernelDelta = ToUInt64(kernel) - ToUInt64(_previousKernel);
        ulong userDelta = ToUInt64(user) - ToUInt64(_previousUser);
        _previousIdle = idle;
        _previousKernel = kernel;
        _previousUser = user;

        ulong total = kernelDelta + userDelta;
        return total == 0 ? 0 : Math.Clamp((total - idleDelta) * 100d / total, 0, 100);
    }

    private static (double percent, string detail) GetMemoryStatus()
    {
        var status = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
        if (!GlobalMemoryStatusEx(ref status) || status.TotalPhys == 0) return (0, "Memory data unavailable");
        ulong used = status.TotalPhys - status.AvailPhys;
        return (used * 100d / status.TotalPhys, $"{FormatBytes(used)} of {FormatBytes(status.TotalPhys)} used");
    }

    private static (double percent, string detail) GetPrimaryDiskUsage()
    {
        try
        {
            DriveInfo? drive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && d.DriveType == DriveType.Fixed);
            if (drive is null || drive.TotalSize == 0) return (0, "No fixed drive found");
            long used = drive.TotalSize - drive.AvailableFreeSpace;
            return (used * 100d / drive.TotalSize, $"{drive.Name} {FormatBytes(used)} of {FormatBytes(drive.TotalSize)} used");
        }
        catch (IOException)
        {
            return (0, "Storage data unavailable");
        }
    }

    private static IReadOnlyList<ProcessInfo> GetProcesses()
    {
        var processes = new List<ProcessInfo>();
        foreach (Process process in Process.GetProcesses())
        {
            try
            {
                processes.Add(new ProcessInfo(process.ProcessName, process.Id, process.WorkingSet64 / 1024d / 1024d, process.Threads.Count, "Running"));
            }
            catch (InvalidOperationException)
            {
                // The process exited while it was being sampled.
            }
            catch (System.ComponentModel.Win32Exception)
            {
                processes.Add(new ProcessInfo(process.ProcessName, process.Id, 0, 0, "Limited"));
            }
            finally
            {
                process.Dispose();
            }
        }
        return processes.OrderByDescending(p => p.MemoryMegabytes).ThenBy(p => p.Name).Take(150).ToList();
    }

    private static ulong ToUInt64(FileTime value) => ((ulong)value.HighDateTime << 32) | value.LowDateTime;
    private static string FormatBytes(ulong bytes) => $"{bytes / 1024d / 1024d / 1024d:N1} GB";
    private static string FormatBytes(long bytes) => FormatBytes((ulong)Math.Max(bytes, 0));

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out FileTime idleTime, out FileTime kernelTime, out FileTime userTime);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct FileTime { public uint LowDateTime; public uint HighDateTime; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }
}
