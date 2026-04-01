using System.Net.Http;
using System.Text.Json;
using VilleneuvoisePlanner.Models;

namespace VilleneuvoisePlanner.Services;

public record GeocodingResult(string Commune, string Street, string Region, string Country);

public class GeocodingService : IDisposable
{
    private readonly HttpClient _http;
    private readonly Dictionary<string, GeocodingResult> _cache = new();
    private const string CacheFile = "geocoding_cache.json";

    public GeocodingService()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("User-Agent", "VilleneuvoisePlanner/1.0 rando-cyclo");
        _http.Timeout = TimeSpan.FromSeconds(15);
        LoadCache();
    }

    private void LoadCache()
    {
        try
        {
            if (File.Exists(CacheFile))
            {
                var json = File.ReadAllText(CacheFile);
                var loaded = JsonSerializer.Deserialize<Dictionary<string, GeocodingResult>>(json);
                if (loaded != null)
                    foreach (var kv in loaded) _cache[kv.Key] = kv.Value;
            }
        }
        catch { /* cache optionnel */ }
    }

    public void SaveCache()
    {
        try { File.WriteAllText(CacheFile, JsonSerializer.Serialize(_cache)); }
        catch { }
    }

    public async Task<List<ScheduleEntry>> GeocodeRouteAsync(
        GpxRoute route,
        AppSettings settings,
        IProgress<(int Current, int Total, string Label)>? progress = null,
        CancellationToken ct = default)
    {
        var sampled = SamplePoints(route.Points, settings.SamplingDistanceKm);
        var entries = new List<ScheduleEntry>();
        string? lastCommune = null, lastStreet = null;
        int total = sampled.Count;
        int current = 0;

        foreach (var point in sampled)
        {
            ct.ThrowIfCancellationRequested();
            current++;

            var key = $"{point.Latitude:F4}_{point.Longitude:F4}";
            GeocodingResult result;
            bool fromCache = _cache.TryGetValue(key, out var cached);
            result = fromCache ? cached! : await FetchAsync(point.Latitude, point.Longitude, ct);

            if (!fromCache)
            {
                _cache[key] = result;
                await Task.Delay(1100, ct); // Nominatim : max 1 req/s
            }

            progress?.Report((current, total, $"{result.Commune} — {result.Street}"));

            if (settings.ExcludedRoads.Contains(result.Street))
                continue;

            if (result.Commune == lastCommune && result.Street == lastStreet)
                continue;

            entries.Add(new ScheduleEntry
            {
                DistanceKm = Math.Round(point.CumulativeDistanceKm, 2),
                Commune = result.Commune,
                Street = result.Street,
                Region = result.Region,
                Country = result.Country
            });

            lastCommune = result.Commune;
            lastStreet = result.Street;
        }

        SaveCache();
        return entries;
    }

    private async Task<GeocodingResult> FetchAsync(double lat, double lon, CancellationToken ct)
    {
        try
        {
            var latStr = lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var lonStr = lon.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={latStr}&lon={lonStr}&accept-language=fr";
            var json = await _http.GetStringAsync(url, ct);
            using var doc = JsonDocument.Parse(json);
            var addr = doc.RootElement.GetProperty("address");

            return new GeocodingResult(
                Commune: Pick(addr, "village", "town", "city", "municipality") ?? "Inconnu",
                Street: Pick(addr, "road", "path", "track", "footway") ?? "Voie inconnue",
                Region: Pick(addr, "state", "region", "county") ?? "Inconnu",
                Country: Pick(addr, "country") ?? "Inconnu"
            );
        }
        catch
        {
            return new GeocodingResult("Inconnu", "Voie inconnue", "Inconnu", "Inconnu");
        }
    }

    private static string? Pick(JsonElement element, params string[] keys)
    {
        foreach (var k in keys)
            if (element.TryGetProperty(k, out var v) && !string.IsNullOrWhiteSpace(v.GetString()))
                return v.GetString();
        return null;
    }

    private static List<GpxPoint> SamplePoints(List<GpxPoint> points, double stepKm)
    {
        if (points.Count == 0) return points;
        var result = new List<GpxPoint> { points[0] };
        double lastKm = 0;
        for (int i = 1; i < points.Count - 1; i++)
        {
            if (points[i].CumulativeDistanceKm - lastKm >= stepKm)
            {
                result.Add(points[i]);
                lastKm = points[i].CumulativeDistanceKm;
            }
        }
        result.Add(points[^1]);
        return result;
    }

    public void Dispose() => _http.Dispose();
}
