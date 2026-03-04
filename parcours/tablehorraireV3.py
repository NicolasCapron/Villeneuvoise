import gpxpy
import pandas as pd
from geopy.geocoders import Nominatim
import time
from tqdm import tqdm  
from pathlib import Path
import ssl
import certifi

BASE_DIR = Path(__file__).resolve().parent

# 📂 Liste des fichiers GPX à traiter
gpx_files = {
    "Gravel 47KM": "gravel_47_klms-20445061-1737809159-847.gpx",
    "Gravel 85KM": "gravel_vers_st_aubert-20439279-1737809122-705.gpx",
    "Circuit Route old": "4_2024_la_villeuvoise_116_kms-15292374-1738056578-82.gpx",
    "Circuit Route ": "4_2024_la_villeuvoise_116_kms-15292374-1738056578-82.gpx"
}

# ⏳ Heure limite d'arrivée
heure_limite_arrivee = pd.to_datetime("13:00:00")

# 🏁 Vitesses définies
vitesse_tete = 30  # km/h (tête de course)
vitesse_milieu = 25  # km/h (milieu de course)
vitesse_fin = 20  # km/h (fin de course)

# 📍 Initialisation du géocodeur avec certificats SSL macOS
ssl_ctx = ssl.create_default_context(cafile=certifi.where())
geolocator = Nominatim(user_agent="rando-cyclo", ssl_context=ssl_ctx)

# 📌 Stockage des données
tables_parcours = {}  # Stockera les DataFrames des parcours
communes_parcours = {}  # Stockera les communes traversées par parcours

# 📌 Boucle sur chaque fichier GPX
for parcours, file in gpx_files.items():
    print(f"\n📍 Traitement de {file} ({parcours})...")

    with open(BASE_DIR / file, "r") as gpx_file:
        gpx = gpxpy.parse(gpx_file)

    total_distance = 0
    prev_point = None
    table_horaire = []
    communes_traversees = set()
    last_commune, last_rue = None, None  

    all_points = [point for track in gpx.tracks for segment in track.segments for point in segment.points]

    # 🕖 Calcul des horaires
    depart_tete = pd.to_datetime("2025-01-01 07:00:00")
    depart_milieu = pd.to_datetime("2025-01-01 08:00:00")
    depart_queue = pd.to_datetime("2025-01-01 09:30:00")

    for point in tqdm(all_points, desc=f"  ⏳ Progression {parcours}", unit=" points"):
        if prev_point:
            total_distance += point.distance_2d(prev_point) / 1000  # en km

        try:
            location = geolocator.reverse((point.latitude, point.longitude), language="fr", timeout=10)
            if location is None:
                commune, rue, pays = "Inconnu", "Rue inconnue", "Inconnu"
            else:
                address = location.raw.get("address", {})
                commune = address.get("village") or address.get("town") or address.get("city", "Inconnu")
                rue = address.get("road", "Rue inconnue")
                pays = address.get("country", "Inconnu")

                # 🚫 Ignorer les points sur une autoroute
                if rue in ["A23", "A1","A22","A27","E42","E403"]:
                    prev_point = point
                    time.sleep(1)
                    continue

        except Exception as e:
            print(f"\n⚠️  Erreur géocodage ({point.latitude:.5f}, {point.longitude:.5f}) : {type(e).__name__}: {e}")
            commune, rue, pays = "Inconnu", "Rue inconnue", "Inconnu"

        # ⚡ Supprimer les doublons consécutifs (même commune et même rue)
        if commune != last_commune or rue != last_rue:
            table_horaire.append([total_distance, commune, rue, pays])
            last_commune, last_rue = commune, rue  

        communes_traversees.add(commune)
        prev_point = point
        time.sleep(1)  # Nominatim : max 1 requête/seconde

    # 📋 Création du DataFrame pour le parcours
    df_horaire = pd.DataFrame(table_horaire, columns=["Distance (km)", "Commune", "Rue", "Pays"])

    # 🕒 Calcul des horaires
    df_horaire["Horaire Tête de course (30 km/h)"] = (
        depart_tete + pd.to_timedelta(df_horaire["Distance (km)"] / vitesse_tete, unit="h")
    ).dt.floor("s").dt.time

    df_horaire["Horaire Milieu de course (25 km/h)"] = (
        depart_milieu + pd.to_timedelta(df_horaire["Distance (km)"] / vitesse_milieu, unit="h")
    ).dt.floor("s").dt.time

    if parcours != "Circuit Route":
        df_horaire["Horaire Fin de course (20 km/h)"] = (
            depart_queue + pd.to_timedelta(df_horaire["Distance (km)"] / vitesse_fin, unit="h")
        ).dt.floor("s").dt.time

        # 🕒 Calcul des horaires en remontant depuis 13h00
        df_horaire[f"Barriere Horaire {vitesse_fin} km/h"] = (
            heure_limite_arrivee - pd.to_timedelta((total_distance - df_horaire["Distance (km)"]) / vitesse_fin, unit="h")
        ).dt.floor("s").dt.time
    else:
         # 🕒 Calcul des horaires en remontant depuis 13h00
        df_horaire[f"Barriere Horaire {vitesse_milieu} km/h"] = (
            heure_limite_arrivee - pd.to_timedelta((total_distance - df_horaire["Distance (km)"]) / vitesse_milieu, unit="h")
        ).dt.floor("s").dt.time

    # 🗂 Stockage des données du parcours
    tables_parcours[parcours] = df_horaire
    communes_parcours[parcours] = sorted(communes_traversees)

# 📤 Générer le fichier Excel avec les parcours séparés
excel_file = "Villeneuvoise_Horaires.xlsx"
with pd.ExcelWriter(BASE_DIR / excel_file, engine="openpyxl") as writer:
    # ✅ Feuilles des parcours
    for parcours, df in tables_parcours.items():
        df.to_excel(writer, sheet_name=parcours, index=False)

    # ✅ Feuille "Communes Traversées" avec 3 tableaux
    max_rows = max(len(communes) for communes in communes_parcours.values())
    df_communes = pd.concat(
        [pd.DataFrame(communes, columns=[f"Communes - {parcours}"]).reindex(range(max_rows)) for parcours, communes in communes_parcours.items()],
        axis=1
    )
    df_communes.to_excel(writer, sheet_name="Communes Traversées", index=False)


print(f"\n✅ Fichier unique généré : {excel_file}")