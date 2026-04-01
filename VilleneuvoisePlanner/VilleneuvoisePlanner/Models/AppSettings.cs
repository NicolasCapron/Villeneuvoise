namespace VilleneuvoisePlanner.Models;

public class AppSettings
{
    public TimeSpan DepartureHead { get; set; } = new(7, 0, 0);
    public TimeSpan DepartureMid { get; set; } = new(8, 0, 0);
    public TimeSpan DepartureTail { get; set; } = new(9, 30, 0);
    public double SpeedHead { get; set; } = 30;
    public double SpeedMid { get; set; } = 25;
    public double SpeedTail { get; set; } = 20;
    public TimeSpan TimeLimit { get; set; } = new(13, 0, 0);
    public double SamplingDistanceKm { get; set; } = 0.1; // 100m entre chaque appel de géocodage
    public List<string> ExcludedRoads { get; set; } = new() { "A23", "A1", "A22", "A27", "E42", "E403" };
}
