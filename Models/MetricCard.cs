using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SystemResourceMonitor.Models;

public sealed class MetricCard : INotifyPropertyChanged
{
    private string _value;
    private double _progress;
    private string _detail;

    public MetricCard(string title, string tag, string subtitle, string value, double progress, string detail, string accent, string accentSurface)
    {
        Title = title;
        Tag = tag;
        Subtitle = subtitle;
        _value = value;
        _progress = progress;
        _detail = detail;
        Accent = accent;
        AccentSurface = accentSurface;
    }

    public string Title { get; }
    public string Tag { get; }
    public string Subtitle { get; }
    public string Accent { get; }
    public string AccentSurface { get; }
    public string Value { get => _value; set => SetField(ref _value, value); }
    public double Progress { get => _progress; set => SetField(ref _progress, value); }
    public string Detail { get => _detail; set => SetField(ref _detail, value); }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
