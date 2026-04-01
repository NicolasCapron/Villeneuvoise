using VilleneuvoisePlanner.Models;

namespace VilleneuvoisePlanner.Services;

public class ScheduleService
{
    public void CalculateSchedule(GpxRoute route, AppSettings settings)
    {
        bool isCircuit = route.Name.Contains("Circuit", StringComparison.OrdinalIgnoreCase);

        foreach (var e in route.ScheduleEntries)
        {
            double km = e.DistanceKm;
            e.HeadTime = settings.DepartureHead + TimeSpan.FromHours(km / settings.SpeedHead);
            e.PelotonTime = settings.DepartureMid + TimeSpan.FromHours(km / settings.SpeedMid);

            double remaining = route.TotalDistanceKm - km;

            if (!isCircuit)
            {
                e.TailTime = settings.DepartureTail + TimeSpan.FromHours(km / settings.SpeedTail);
                e.TimeBarrier = settings.TimeLimit - TimeSpan.FromHours(remaining / settings.SpeedTail);
            }
            else
            {
                e.TimeBarrier = settings.TimeLimit - TimeSpan.FromHours(remaining / settings.SpeedMid);
            }
        }

        route.CommuneKmEntries = ComputeKmParCommune(route);
        route.IsProcessed = true;
        route.IsProcessing = false;
    }

    private static List<CommuneKmEntry> ComputeKmParCommune(GpxRoute route)
    {
        var result = new List<CommuneKmEntry>();
        var entries = route.ScheduleEntries;
        int i = 0;
        while (i < entries.Count)
        {
            string commune = entries[i].Commune;
            double entry = entries[i].DistanceKm;
            int j = i + 1;
            while (j < entries.Count && entries[j].Commune == commune) j++;
            double exit = j < entries.Count ? entries[j].DistanceKm : route.TotalDistanceKm;
            result.Add(new CommuneKmEntry
            {
                Commune = commune,
                EntryKm = Math.Round(entry, 2),
                ExitKm = Math.Round(exit, 2),
                AvgKm = Math.Round((entry + exit) / 2, 2),
                TerritoryDistanceKm = Math.Round(exit - entry, 2)
            });
            i = j;
        }
        return result;
    }
}
