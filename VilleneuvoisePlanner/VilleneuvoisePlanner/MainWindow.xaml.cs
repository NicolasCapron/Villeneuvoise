using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using NetTopologySuite.Geometries;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using VilleneuvoisePlanner.Models;
using VilleneuvoisePlanner.ViewModels;

namespace VilleneuvoisePlanner;

public partial class MainWindow : Window
{
    private readonly Map _map = new();
    private MainViewModel Vm => (MainViewModel)DataContext;

    // Couleurs Mapsui correspondant aux RouteColors du ViewModel
    private static readonly Mapsui.Styles.Color[] MapColors =
    {
        new() { R = 231, G = 76,  B = 60,  A = 220 }, // rouge
        new() { R = 52,  G = 152, B = 219, A = 220 }, // bleu
        new() { R = 46,  G = 204, B = 113, A = 220 }, // vert
        new() { R = 243, G = 156, B = 18,  A = 220 }, // orange
        new() { R = 155, G = 89,  B = 182, A = 220 }, // violet
        new() { R = 26,  G = 188, B = 156, A = 220 }, // teal
        new() { R = 230, G = 126, B = 34,  A = 220 }, // orange foncé
        new() { R = 41,  G = 128, B = 185, A = 220 }, // bleu foncé
    };

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Initialiser la carte avec le fond OSM
        _map.Layers.Add(OpenStreetMap.CreateTileLayer());
        mapControl.Map = _map;

        // Centrer sur la France/Belgique (zone Villeneuve d'Ascq)
        var center = SphericalMercator.FromLonLat(3.2, 50.6);
        _map.Navigator.CenterOn(new MPoint(center.x, center.y));
        _map.Navigator.ZoomTo(2000); // ~zoom 10

        // Écouter les changements de collection de routes
        Vm.Routes.CollectionChanged += (_, _) => RefreshMapLayers();
        Vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(Vm.SelectedRoute))
                HighlightSelectedRoute();
        };
    }

    // ── Carte ────────────────────────────────────────────────────────────────

    private void RefreshMapLayers()
    {
        // Supprimer toutes les couches de routes
        var toRemove = _map.Layers.Where(l => l.Name?.StartsWith("route_") == true).ToList();
        foreach (var l in toRemove) _map.Layers.Remove(l);

        // Ajouter une couche par route
        for (int i = 0; i < Vm.Routes.Count; i++)
        {
            var route = Vm.Routes[i];
            if (route.Points.Count < 2) continue;

            var color = MapColors[i % MapColors.Length];
            var layer = BuildRouteLayer(route, color, 3f);
            layer.Name = $"route_{route.FilePath}";
            _map.Layers.Add(layer);
        }

        mapControl.Refresh();
    }

    private void HighlightSelectedRoute()
    {
        RefreshMapLayers();
        var sel = Vm.SelectedRoute;
        if (sel == null) return;

        // Surbrillance de la route sélectionnée
        var idx = Vm.Routes.IndexOf(sel);
        var layerName = $"route_{sel.FilePath}";
        var layer = _map.Layers.FirstOrDefault(l => l.Name == layerName);
        if (layer != null)
        {
            _map.Layers.Remove(layer);
            var highlight = BuildRouteLayer(sel, MapColors[idx % MapColors.Length], 5f);
            highlight.Name = layerName;
            _map.Layers.Add(highlight);
        }
        mapControl.Refresh();
    }

    private static MemoryLayer BuildRouteLayer(GpxRoute route, Mapsui.Styles.Color color, float width)
    {
        var coords = route.Points
            .Select(p => SphericalMercator.FromLonLat(p.Longitude, p.Latitude))
            .Select(p => new Coordinate(p.x, p.y))
            .ToArray();

        var feature = new GeometryFeature(new LineString(coords));

        return new MemoryLayer
        {
            Features = new[] { feature },
            Style = new VectorStyle { Line = new Pen(color, width) }
        };
    }

    private void RouteList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        HighlightSelectedRoute();
    }

    private void BtnCenter_Click(object sender, RoutedEventArgs e)
    {
        var route = Vm.SelectedRoute;
        if (route == null || route.Points.Count == 0) return;
        ZoomToRoute(route);
    }

    private void BtnFitAll_Click(object sender, RoutedEventArgs e)
    {
        if (Vm.Routes.Count == 0) return;
        var allPoints = Vm.Routes.SelectMany(r => r.Points).ToList();
        ZoomToPoints(allPoints);
    }

    private void ZoomToRoute(GpxRoute route) => ZoomToPoints(route.Points);

    private void ZoomToPoints(List<GpxPoint> points)
    {
        if (points.Count == 0) return;

        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;

        foreach (var p in points)
        {
            var mp = SphericalMercator.FromLonLat(p.Longitude, p.Latitude);
            if (mp.x < minX) minX = mp.x;
            if (mp.y < minY) minY = mp.y;
            if (mp.x > maxX) maxX = mp.x;
            if (mp.y > maxY) maxY = mp.y;
        }

        var bbox = new MRect(minX, minY, maxX, maxY);
        _map.Navigator.ZoomToBox(bbox.Grow(bbox.Width * 0.05));
        mapControl.Refresh();
    }
}
