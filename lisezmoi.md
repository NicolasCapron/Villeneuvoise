# Villeneuvoise — Mise en route du script

## Prérequis

- **Python 3.10+** installé (le script utilise Python 3.14 ici, mais 3.10+ suffit)
- **Git** ou un simple transfert de dossier

---

## Étapes

### 1. Copier les fichiers

Transférer le dossier `Villeneuvoise/` avec au minimum :
```
parcours/
├── tablehorraireV3.py
├── gravel_47_klms-20445061-1737809159-847.gpx
├── gravel_vers_st_aubert-20439279-1737809122-705.gpx
└── 4_2024_la_villeuvoise_116_kms-15292374-1738056578-82.gpx
```

### 2. Créer un virtualenv

```bash
cd Villeneuvoise
python3 -m venv .venv
```

### 3. Activer le virtualenv

```bash
# macOS / Linux
source .venv/bin/activate

# Windows
.venv\Scripts\activate
```

### 4. Installer les dépendances

```bash
pip install gpxpy pandas geopy tqdm openpyxl
```

### 5. Lancer le script

```bash
python parcours/tablehorraireV3.py
```

---

## Notes importantes

- **Durée d'exécution longue** : le script fait un appel réseau Nominatim par point GPS avec `time.sleep(0.2)`. Sur 4 parcours (~4 000–6 000 points au total), comptez **plusieurs heures**.
- **Connexion internet requise** : le géocodeur Nominatim interroge les serveurs OpenStreetMap en ligne.
- **Fichier de sortie** : `parcours/Villeneuvoise_Horaires.xlsx` est généré dans le même dossier que le script.

---

## Conseil : figer les dépendances

Créer un fichier `requirements.txt` dans le projet pour simplifier l'installation à l'avenir :

```bash
pip freeze > requirements.txt
# puis sur l'autre machine :
pip install -r requirements.txt
```
