namespace VilleneuvoisePlanner.Models;

public class CommuneKmEntry
{
    public string Commune { get; set; } = string.Empty;
    public double EntryKm { get; set; }
    public double ExitKm { get; set; }
    public double AvgKm { get; set; }
    public double TerritoryDistanceKm { get; set; }
}
