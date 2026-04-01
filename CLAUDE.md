# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Context

Ce dépôt contient deux choses :

1. **`tablehorraireV3.py`** — script Python originel qui lit des fichiers GPX, les géocode via Nominatim et génère un tableau horaire Excel pour la randonnée cycliste *La Villeneuvoise*.
2. **`VilleneuvoisePlanner/`** — application WPF .NET 7 équivalente avec interface graphique, générée à partir du script Python.

Les fichiers GPX des parcours sont dans `parcours/`.

## Commandes

### .NET / WPF (application principale)

```bash
# Build (cross-compile depuis macOS, cible Windows)
export PATH="$HOME/.dotnet:$PATH"   # SDK 8 installé dans ~/.dotnet
dotnet build VilleneuvoisePlanner/VilleneuvoisePlanner/VilleneuvoisePlanner.csproj

# Restore des packages NuGet
dotnet restore VilleneuvoisePlanner/VilleneuvoisePlanner/VilleneuvoisePlanner.csproj

# Lancer le SDK 8 sur macOS (installé sans sudo via dotnet-install.sh)
dotnet run --project VilleneuvoisePlanner/VilleneuvoisePlanner

# Lancer sur Windows
dotnet run --project VilleneuvoisePlanner/VilleneuvoisePlanner
```

> Le SDK .NET 8 est installé dans `~/.dotnet` (via `dotnet-install.sh`, sans sudo). Il n'est pas dans le PATH par défaut — toujours préfixer avec `export PATH="$HOME/.dotnet:$PATH"`.
> La propriété `<EnableWindowsTargeting>true</EnableWindowsTargeting>` est requise pour compiler une cible `net7.0-windows` depuis macOS.

### Python (script originel)

```bash
python3 -m venv .venv && source .venv/bin/activate
pip install -r requierement.txt
python tablehorraireV3.py
```

Le script génère `parcours/Villeneuvoise_Horaires.xlsx`. Il est très lent (1 req/s Nominatim sur chaque point GPS, plusieurs heures par exécution).

## Architecture WPF

L'application suit le pattern **MVVM** avec `CommunityToolkit.Mvvm`.

### Flux de données

```
GPX file → GpxParserService → GpxRoute (Points[])
                                    ↓
                         GeocodingService (Nominatim async)
                                    ↓
                         GpxRoute.ScheduleEntries[]
                                    ↓
                         ScheduleService → horaires + CommuneKmEntries[]
                                    ↓
                    MainViewModel (ObservableCollection<GpxRoute>)
                         ↙              ↘
              MainWindow.xaml.cs      ExcelExportService
              (map layers via Mapsui)   (ClosedXML → .xlsx)
```

### Points d'attention

**Mapsui** : `SphericalMercator.FromLonLat(lon, lat)` retourne un ValueTuple `(double x, double y)` avec champs **minuscules** `.x` et `.y`. Les couches de routes sont des `MemoryLayer` avec `GeometryFeature` (NTS `LineString`). La map est gérée entièrement dans le code-behind `MainWindow.xaml.cs` (pas dans le ViewModel) car `MapControl` nécessite un accès direct.

**Géocodage** : `GeocodingService` échantillonne les points GPX tous les 100 m (paramètre `AppSettings.SamplingDistanceKm`) pour limiter les appels API. Un cache JSON disque (`geocoding_cache.json`) est sauvegardé à chaque fin de traitement pour ne pas re-géocoder les mêmes coordonnées.

**`GpxRoute` est un `ObservableObject`** (pas un simple POCO) pour que `IsProcessed` et `IsProcessing` mettent à jour la ListBox en temps réel pendant le géocodage.

**Détection "Circuit Route"** dans `ScheduleService` : si le nom du parcours contient "Circuit", la vitesse de barrière horaire utilisée est celle du milieu (25 km/h) au lieu de la queue (20 km/h), comme dans le script Python originel.

### Packages NuGet clés

| Package | Usage |
|---|---|
| `Mapsui` + `Mapsui.Wpf` + `Mapsui.Nts` + `Mapsui.Tiling` 4.1.7 | Carte OSM + tracés GPX |
| `ClosedXML` 0.102.3 | Export Excel |
| `CommunityToolkit.Mvvm` 8.3.2 | ObservableObject, RelayCommand, ObservableProperty |
