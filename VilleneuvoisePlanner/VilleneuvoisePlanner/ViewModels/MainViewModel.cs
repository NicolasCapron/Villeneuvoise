using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using VilleneuvoisePlanner.Models;
using VilleneuvoisePlanner.Services;

namespace VilleneuvoisePlanner.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private static readonly string[] RouteColors =
        { "#E74C3C", "#3498DB", "#2ECC71", "#F39C12", "#9B59B6", "#1ABC9C", "#E67E22", "#2980B9" };

    private readonly GpxParserService _parser = new();
    private readonly GeocodingService _geocoder = new();
    private readonly ScheduleService _scheduler = new();
    private readonly ExcelExportService _excelExport = new();

    [ObservableProperty] private ObservableCollection<GpxRoute> _routes = new();
    [ObservableProperty] private GpxRoute? _selectedRoute;
    [ObservableProperty] private AppSettings _settings = new();
    [ObservableProperty] private string _statusText = "Prêt — importez un ou plusieurs fichiers GPX.";
    [ObservableProperty] private int _progressValue;
    [ObservableProperty] private int _progressMax = 100;
    [ObservableProperty] private bool _isProcessing;

    private CancellationTokenSource? _cts;

    public string ProcessButtonText => IsProcessing ? "⏹ Annuler" : "⚙ Géocoder";

    partial void OnIsProcessingChanged(bool value) => OnPropertyChanged(nameof(ProcessButtonText));

    // ── Import ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void ImportGpx()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Importer des fichiers GPX",
            Filter = "Fichiers GPX (*.gpx)|*.gpx",
            Multiselect = true
        };
        if (dlg.ShowDialog() != true) return;

        foreach (var file in dlg.FileNames)
        {
            if (Routes.Any(r => r.FilePath == file)) continue;
            try
            {
                var route = _parser.Parse(file);
                route.RouteColor = RouteColors[Routes.Count % RouteColors.Length];
                Routes.Add(route);
                StatusText = $"Chargé : {route.Name} ({route.TotalDistanceKm:F1} km, {route.Points.Count} points)";
            }
            catch (Exception ex)
            {
                StatusText = $"Erreur lecture {Path.GetFileName(file)} : {ex.Message}";
            }
        }

        SelectedRoute ??= Routes.FirstOrDefault();
    }

    [RelayCommand]
    private void RemoveRoute()
    {
        if (SelectedRoute == null || IsProcessing) return;
        Routes.Remove(SelectedRoute);
        SelectedRoute = Routes.FirstOrDefault();
    }

    // ── Géocodage ───────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ProcessAllRoutesAsync()
    {
        if (IsProcessing) { _cts?.Cancel(); return; }

        var todo = Routes.Where(r => !r.IsProcessed).ToList();
        if (todo.Count == 0) { StatusText = "Tous les parcours sont déjà traités."; return; }

        IsProcessing = true;
        _cts = new CancellationTokenSource();

        try
        {
            foreach (var route in todo)
            {
                route.IsProcessing = true;
                ProgressMax = route.Points.Count;
                ProgressValue = 0;

                var progress = new Progress<(int C, int T, string L)>(p =>
                {
                    ProgressValue = p.C;
                    StatusText = $"{route.Name} : {p.C}/{p.T} — {p.L}";
                });

                route.ScheduleEntries = await _geocoder.GeocodeRouteAsync(
                    route, Settings, progress, _cts.Token);

                _scheduler.CalculateSchedule(route, Settings);
                StatusText = $"{route.Name} : {route.ScheduleEntries.Count} entrées générées.";
            }
            StatusText = "Traitement terminé.";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Traitement annulé.";
            foreach (var r in todo) r.IsProcessing = false;
        }
        catch (Exception ex)
        {
            StatusText = $"Erreur : {ex.Message}";
            foreach (var r in todo) r.IsProcessing = false;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private void RecalculateSchedule()
    {
        foreach (var route in Routes.Where(r => r.IsProcessed))
            _scheduler.CalculateSchedule(route, Settings);
        StatusText = "Horaires recalculés avec les nouveaux paramètres.";
    }

    // ── Export ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void ExportToExcel()
    {
        var processed = Routes.Where(r => r.IsProcessed).ToList();
        if (processed.Count == 0) { StatusText = "Aucun parcours traité à exporter."; return; }

        var dlg = new SaveFileDialog
        {
            Title = "Exporter vers Excel",
            Filter = "Fichier Excel (*.xlsx)|*.xlsx",
            FileName = "Villeneuvoise_Horaires.xlsx"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            _excelExport.Export(processed, dlg.FileName);
            StatusText = $"Export réussi : {dlg.FileName}";
        }
        catch (Exception ex)
        {
            StatusText = $"Erreur export : {ex.Message}";
        }
    }
}
