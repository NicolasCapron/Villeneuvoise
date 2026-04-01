namespace VilleneuvoisePlanner.Models;

public class ScheduleEntry
{
    public double DistanceKm { get; set; }
    public string Commune { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public TimeSpan? HeadTime { get; set; }
    public TimeSpan? PelotonTime { get; set; }
    public TimeSpan? TailTime { get; set; }
    public TimeSpan? TimeBarrier { get; set; }

    public string DistanceStr => $"{DistanceKm:F2}";
    public string HeadTimeStr => HeadTime?.ToString(@"hh\:mm\:ss") ?? "";
    public string PelotonTimeStr => PelotonTime?.ToString(@"hh\:mm\:ss") ?? "";
    public string TailTimeStr => TailTime?.ToString(@"hh\:mm\:ss") ?? "";
    public string TimeBarrierStr => TimeBarrier?.ToString(@"hh\:mm\:ss") ?? "";
}
