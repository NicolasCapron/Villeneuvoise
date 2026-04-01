using CommunityToolkit.Mvvm.ComponentModel;

namespace VilleneuvoisePlanner.Models;

public partial class GpxRoute : ObservableObject
{
    [ObservableProperty] private bool _isProcessed;
    [ObservableProperty] private bool _isProcessing;
    [ObservableProperty] private List<ScheduleEntry> _scheduleEntries = new();
    [ObservableProperty] private List<CommuneKmEntry> _communeKmEntries = new();

    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public List<GpxPoint> Points { get; set; } = new();
    public double TotalDistanceKm { get; set; }
    public string RouteColor { get; set; } = "#E74C3C";

    public string DisplayInfo => $"{TotalDistanceKm:F1} km — {Points.Count} pts";
}
