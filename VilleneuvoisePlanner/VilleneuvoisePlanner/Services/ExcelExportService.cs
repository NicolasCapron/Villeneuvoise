using ClosedXML.Excel;
using VilleneuvoisePlanner.Models;

namespace VilleneuvoisePlanner.Services;

public class ExcelExportService
{
    public void Export(List<GpxRoute> routes, string filePath)
    {
        using var wb = new XLWorkbook();
        var processed = routes.Where(r => r.IsProcessed).ToList();

        foreach (var route in processed)
            AddScheduleSheet(wb, route);

        AddCommunesSheet(wb, processed);
        AddKmParCommuneSheet(wb, processed);

        wb.SaveAs(filePath);
    }

    private static void AddScheduleSheet(XLWorkbook wb, GpxRoute route)
    {
        var ws = wb.Worksheets.Add(Sanitize(route.Name));
        string[] headers = { "Distance (km)", "Commune", "Rue", "Région", "Pays",
            "Tête de course (30 km/h)", "Milieu (25 km/h)", "Queue (20 km/h)", "Barrière horaire" };

        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cell(1, c + 1).Value = headers[c];
            ws.Cell(1, c + 1).Style.Font.Bold = true;
            ws.Cell(1, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            ws.Cell(1, c + 1).Style.Font.FontColor = XLColor.White;
        }

        int row = 2;
        foreach (var e in route.ScheduleEntries)
        {
            ws.Cell(row, 1).Value = e.DistanceKm;
            ws.Cell(row, 2).Value = e.Commune;
            ws.Cell(row, 3).Value = e.Street;
            ws.Cell(row, 4).Value = e.Region;
            ws.Cell(row, 5).Value = e.Country;
            ws.Cell(row, 6).Value = e.HeadTimeStr;
            ws.Cell(row, 7).Value = e.PelotonTimeStr;
            ws.Cell(row, 8).Value = e.TailTimeStr;
            ws.Cell(row, 9).Value = e.TimeBarrierStr;
            row++;
        }

        ws.Columns().AdjustToContents();
    }

    private static void AddCommunesSheet(XLWorkbook wb, List<GpxRoute> routes)
    {
        var ws = wb.Worksheets.Add("Communes Traversées");
        int col = 1;
        foreach (var route in routes)
        {
            ws.Cell(1, col).Value = $"Communes — {route.Name}";
            ws.Cell(1, col).Style.Font.Bold = true;
            var communes = route.ScheduleEntries.Select(e => e.Commune).Distinct().OrderBy(c => c).ToList();
            for (int i = 0; i < communes.Count; i++)
                ws.Cell(i + 2, col).Value = communes[i];
            col++;
        }
        ws.Columns().AdjustToContents();
    }

    private static void AddKmParCommuneSheet(XLWorkbook wb, List<GpxRoute> routes)
    {
        var ws = wb.Worksheets.Add("Km par Commune");
        int col = 1;
        foreach (var route in routes)
        {
            string[] heads = { $"Commune — {route.Name}", $"Km entrée", $"Km sortie", $"Km moyen", $"Distance territoire" };
            for (int i = 0; i < heads.Length; i++)
            {
                ws.Cell(1, col + i).Value = heads[i];
                ws.Cell(1, col + i).Style.Font.Bold = true;
            }
            int row = 2;
            foreach (var e in route.CommuneKmEntries)
            {
                ws.Cell(row, col).Value = e.Commune;
                ws.Cell(row, col + 1).Value = e.EntryKm;
                ws.Cell(row, col + 2).Value = e.ExitKm;
                ws.Cell(row, col + 3).Value = e.AvgKm;
                ws.Cell(row, col + 4).Value = e.TerritoryDistanceKm;
                row++;
            }
            col += 5;
        }
        ws.Columns().AdjustToContents();
    }

    private static string Sanitize(string name)
    {
        foreach (var c in new[] { ':', '\\', '/', '?', '*', '[', ']' })
            name = name.Replace(c, '-');
        return name.Length > 31 ? name[..31] : name;
    }
}
