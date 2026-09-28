using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using SystemResourceMonitor.Models;
using SystemResourceMonitor.Services;

namespace SystemResourceMonitor;

public partial class MainWindow : Window
{
    private readonly ResourceMonitorService _monitorService = new();
    private readonly DispatcherTimer _refreshTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    private readonly ObservableCollection<MetricCard> _metrics;
    private readonly ObservableCollection<ProcessInfo> _processes = [];
    private readonly ICollectionView _processesView;
    private bool _isMonitoring = true;
    private string _processSearchText = string.Empty;

    public MainWindow()
    {
        InitializeComponent();
        _metrics =
        [
            new("CPU usage", "CPU", "Current processor load", "0%", 0, "Collecting first sample...", "#4ADE80", "#163B2B"),
            new("Memory", "RAM", "Physical memory in use", "0%", 0, "Reading memory...", "#67D5FF", "#123342"),
            new("Disk capacity", "DISK", "Primary fixed drive", "0%", 0, "Checking storage...", "#F6B658", "#3A2B18")
        ];
        _processesView = CollectionViewSource.GetDefaultView(_processes);
        _processesView.Filter = FilterProcess;
        DataContext = this;
        _refreshTimer.Tick += async (_, _) => await RefreshAsync();
        Loaded += async (_, _) => { await RefreshAsync(); _refreshTimer.Start(); };
        Closed += (_, _) => _refreshTimer.Stop();
    }

    public ObservableCollection<MetricCard> Metrics => _metrics;
    public ICollectionView ProcessesView => _processesView;
    public string ProcessSearchText
    {
        get => _processSearchText;
        set { _processSearchText = value; _processesView.Refresh(); UpdateProcessCount(); }
    }

    private async Task RefreshAsync()
    {
        if (!_isMonitoring) return;
        try { ApplySnapshot(await Task.Run(_monitorService.GetSnapshot)); }
        catch (Exception ex) { StatusText.Text = $"Refresh failed: {ex.Message}"; }
    }

    private void ApplySnapshot(MonitoringSnapshot snapshot)
    {
        UpdateMetric(_metrics[0], snapshot.CpuPercent, $"{snapshot.CpuPercent:F0}%", "Processor load");
        UpdateMetric(_metrics[1], snapshot.MemoryPercent, $"{snapshot.MemoryPercent:F0}%", snapshot.MemoryDetail);
        UpdateMetric(_metrics[2], snapshot.DiskPercent, $"{snapshot.DiskPercent:F0}%", snapshot.DiskDetail);
        _processes.Clear();
        foreach (var process in snapshot.Processes) _processes.Add(process);
        _processesView.Refresh();
        UpdateProcessCount();
        StatusText.Text = $"Live  |  Last refreshed {snapshot.CollectedAt:h:mm:ss tt}";
    }

    private static void UpdateMetric(MetricCard metric, double percent, string value, string detail)
    {
        metric.Progress = percent;
        metric.Value = value;
        metric.Detail = detail;
    }

    private bool FilterProcess(object item)
    {
        if (item is not ProcessInfo process) return false;
        return string.IsNullOrWhiteSpace(_processSearchText)
            || process.Name.Contains(_processSearchText, StringComparison.OrdinalIgnoreCase)
            || process.Id.ToString().Contains(_processSearchText, StringComparison.Ordinal);
    }

    private void UpdateProcessCount() => ProcessCountText.Text = $"{ProcessesView.Cast<object>().Count()} processes shown";
    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshAsync();

    private async void ToggleMonitoring_Click(object sender, RoutedEventArgs e)
    {
        _isMonitoring = !_isMonitoring;
        RefreshButton.IsEnabled = _isMonitoring;
        PauseButtonText.Text = _isMonitoring ? "Pause monitoring" : "Resume monitoring";
        LiveDot.Fill = _isMonitoring ? (Brush)FindResource("GreenAccent") : (Brush)FindResource("MutedText");
        StatusText.Text = _isMonitoring ? "Monitoring resumed" : "Monitoring paused";
        if (_isMonitoring) await RefreshAsync();
    }

    private void ProcessGrid_Sorting(object sender, DataGridSortingEventArgs e)
    {
        e.Handled = true;
        _processesView.SortDescriptions.Clear();
        _processesView.SortDescriptions.Add(new SortDescription(e.Column.SortMemberPath, ListSortDirection.Descending));
    }

    private void ProcessGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ProcessGrid.SelectedItem is not ProcessInfo process) return;
        MessageBox.Show(this, $"{process.Name}\nPID: {process.Id}\nMemory: {process.MemoryDisplay}\nThreads: {process.ThreadCount}\nStatus: {process.Status}", "Process details", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
