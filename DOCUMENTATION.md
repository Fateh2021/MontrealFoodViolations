# Registre alimentaire — Guide d'exécution et documentation API

Ce document explique comment lancer le projet, utiliser l'interface web et consommer chaque endpoint de l'API REST.

---

## Présentation

**Registre alimentaire** est une application ASP.NET Core qui :

- télécharge automatiquement le CSV officiel des infractions alimentaires de la Ville de Montréal ;
- stocke les données dans une base **SQLite** ;
- expose une **API REST** pour consulter, rechercher et exporter les infractions ;
- propose une **interface web Vue 3** de recherche et de statistiques.

**Source officielle :** https://donnees.montreal.ca/dataset/inspection-aliments-contrevenants

---

## Prérequis

- .NET SDK 10.x
- Node.js (seulement pour modifier l'interface Vue 3)
- Connexion Internet (synchronisation initiale)

```bash
dotnet --version
```

---

## Exécution

```bash
cd RegistreAlimentaire
dotnet run --project src/RegistreAlimentaire.Api/RegistreAlimentaire.Api.csproj
```

Interface locale : **http://localhost:5067**

Site déployé : **https://montrealfoodviolations-bsfrgnhtfygrduhu.canadaeast-01.azurewebsites.net**

Swagger : **`/swagger`** (local et production)

Au démarrage, les migrations EF Core sont appliquées automatiquement et une synchronisation des données est lancée en arrière-plan.

L'interface est une application **Vue 3** (Composition API) dans `frontend/`. Le build de production est servi par l'API depuis `wwwroot`. Pour modifier l'UI :

```bash
cd frontend
npm install
npm run build
```

En développement UI uniquement : lancer l'API, puis `npm run dev` dans `frontend/` (Vite proxifie `/api` vers `http://localhost:5067`). Node.js n'est pas requis pour simplement exécuter le projet.

---

## Interface web

- Le menu **Ville** envoie `ville` à la liste, à l'export et à `/api/violations/stats`. `Toutes` compte toutes les villes. `Montréal` limite à l'agglomération. Une autre valeur, par exemple `Laval`, ne garde que cette municipalité. Laval, Longueuil, Québec et Gatineau sont groupées sous **Villes fréquentes**.
- L'interface traduit les catégories abrégées du MAPAQ. Les valeurs stockées ne changent pas.
- Sur une page de résultats, l'amende la plus élevée est encadrée si les montants ne sont pas tous identiques. La fiche établissement ajoute une phrase de synthèse.
- Les années antérieures à 2024 ne viennent que de Montréal. Le fichier du MAPAQ couvre les 24 derniers mois.
- La recherche commence par un champ **Nom ou adresse**. Il envoie le paramètre `search`. Les filtres établissement, adresse, catégorie, statut, propriétaire et description sont sous **Recherche avancée**.
- Les colonnes visibles commencent par l'établissement, l'adresse, la date, la date de jugement, le montant et le statut. Les dates du calendrier sont en français.
- Le tri initial est `Date` décroissant.
- `id_poursuite` et `business_id` ne sont pas affichés. Le clic sur un établissement utilise encore `businessId` pour ouvrir la fiche.
- Sous 720 px de large, des fiches remplacent le tableau. Chaque fiche montre le nom, l'adresse, la date, le montant, le statut et la catégorie.

---

## Configuration

Fichier : `src/RegistreAlimentaire.Api/appsettings.json`

| Paramètre | Description |
|-----------|-------------|
| `ConnectionStrings:DefaultConnection` | Chemin SQLite (`registrealimentaire.db`) |
| `ViolationSync:Enabled` | Sync automatique activée/désactivée |
| `ViolationSync:IntervalHours` | Intervalle entre syncs (défaut : 24 h) |
| `MontrealDataset:ViolationsUrl` | URL du CSV officiel |

---

## Endpoints API

Base URL : `http://localhost:5067`

### Violations

| Méthode | Endpoint | Description |
|---------|----------|-------------|
| `GET` | `/api/violations` | Liste paginée |
| `GET` | `/api/violations/{id}` | Détail par `id_poursuite` |
| `GET` | `/api/violations/business/{businessId}` | Fiche établissement + historique |
| `GET` | `/api/violations/search` | Recherche avancée avec filtres et tri |
| `GET` | `/api/violations/export` | Export CSV (max 5 000 lignes) |
| `GET` | `/api/violations/cities` | Villes importées du MAPAQ |
| `GET` | `/api/violations/stats` | Statistiques des amendes, filtrables par `ville` |

#### Paramètres de recherche (`/search` et `/export`)

- `search`, `etablissement`, `adresse`, `categorie`, `statut`, `proprietaire`, `description`, `ville`
- `page`, `pageSize` (max 200), `sortBy`, `descending`

**Tri (`sortBy`) :** `IdPoursuite`, `BusinessId`, `Etablissement`, `Adresse`, `Ville`, `Categorie`, `Statut`, `Proprietaire`, `Montant`, `Description`, `Date`, `DateJugement`

### Synchronisation

| Méthode | Endpoint | Description |
|---------|----------|-------------|
| `POST` | `/api/sync` | Déclenche une sync manuelle |
| `GET` | `/api/sync/status` | État de la dernière synchronisation |

---

## Modèle `Violation`

| Champ | Description |
|-------|-------------|
| `idPoursuite` | Identifiant unique (clé primaire) |
| `businessId` | Identifiant Ville de l'établissement |
| `etablissement` | Nom du commerce |
| `adresse`, `ville` | Localisation |
| `categorie`, `description` | Type et détail de l'infraction |
| `statut` | Statut de l'établissement (Ouvert, Fermé, etc.) |
| `montant` | Amende imposée (CAD) |
| `date`, `dateJugement` | Dates clés |

> Le champ `montant` correspond à l'amende **imposée**, pas à un statut de paiement.

---

## Tests

```bash
dotnet test RegistreAlimentaire.sln
```

---

## Architecture

```
frontend/                                # Interface Vue 3 (Vite)
src/
├── RegistreAlimentaire.Api/             # API REST + wwwroot (build Vue)
├── RegistreAlimentaire.Application/     # Interfaces, modèles, options
├── RegistreAlimentaire.Domain/          # Entités métier
└── RegistreAlimentaire.Infrastructure/  # EF Core, sync, parsing CSV
tests/
└── RegistreAlimentaire.Tests/
```
