using System.IO;
using System.Xml.Linq;
using VilleneuvoisePlanner.Models;

namespace VilleneuvoisePlanner.Services;

public class GpxParserService
{
    private static readonly XNamespace Ns = "http://www.topografix.com/GPX/1/1";

    public GpxRoute Parse(string filePath)
    {
        var doc = XDocument.Load(filePath);
        var routeName = Path.GetFileNameWithoutExtension(filePath);

        var nameEl = doc.Root?.Element(Ns + "metadata")?.Element(Ns + "name")
                  ?? doc.Root?.Element(Ns + "trk")?.Element(Ns + "name");
        if (nameEl != null && !string.IsNullOrWhiteSpace(nameEl.Value))
            routeName = nameEl.Value.Trim();

        var points = new List<GpxPoint>();
        double totalDistance = 0;
        GpxPoint? prev = null;

        foreach (var trkpt in doc.Descendants(Ns + "trkpt"))
        {
            double lat = double.Parse(trkpt.Attribute("lat")!.Value, System.Globalization.CultureInfo.InvariantCulture);
            double lon = double.Parse(trkpt.Attribute("lon")!.Value, System.Globalization.CultureInfo.InvariantCulture);

            if (prev != null)
                totalDistance += Haversine(prev.Latitude, prev.Longitude, lat, lon);

            var pt = new GpxPoint { Latitude = lat, Longitude = lon, CumulativeDistanceKm = totalDistance };
            points.Add(pt);
            prev = pt;
        }

        return new GpxRoute
        {
            Name = routeName,
            FilePath = filePath,
            Points = points,
            TotalDistanceKm = totalDistance
        };
    }

    public static double Haversine(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0;
        double dLat = (lat2 - lat1) * Math.PI / 180;
        double dLon = (lon2 - lon1) * Math.PI / 180;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                 + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
                 * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}
