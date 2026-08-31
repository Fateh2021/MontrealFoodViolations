# Montreal Food Violations — Guide d'exécution et documentation API

Ce document explique comment lancer le projet, utiliser l'interface web et consommer chaque endpoint de l'API REST.

---

## Présentation

**Montreal Food Violations** est une application ASP.NET Core qui :

- télécharge automatiquement le CSV officiel des infractions alimentaires de la Ville de Montréal ;
- stocke les données dans une base **SQLite** ;
- expose une **API REST** pour consulter, rechercher et exporter les infractions ;
- propose une **interface web** de recherche et de statistiques.

**Source officielle :** https://donnees.montreal.ca/dataset/inspection-aliments-contrevenants

---

## Prérequis

- .NET SDK 10.x
- Connexion Internet (synchronisation initiale)

```bash
dotnet --version
```

---

## Exécution

```bash
cd MontrealFoodViolations
dotnet run --project src/MontrealFoodViolations.Api/MontrealFoodViolations.Api.csproj
```

Interface web : **http://localhost:5067**

Au démarrage, les migrations EF Core sont appliquées automatiquement et une synchronisation des données est lancée en arrière-plan.

---

## Configuration

Fichier : `src/MontrealFoodViolations.Api/appsettings.json`

| Paramètre | Description |
|-----------|-------------|
| `ConnectionStrings:DefaultConnection` | Chemin SQLite (`montrealfoodviolations.db`) |
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
| `GET` | `/api/violations/stats` | Statistiques globales et amendes |

#### Paramètres de recherche (`/search` et `/export`)

- `search`, `etablissement`, `adresse`, `categorie`, `statut`, `proprietaire`, `description`
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
dotnet test MontrealFoodViolations.sln
```

---

## Architecture

```
src/
├── MontrealFoodViolations.Api/           # API REST + interface web
├── MontrealFoodViolations.Application/   # Interfaces, modèles, options
├── MontrealFoodViolations.Domain/        # Entités métier
└── MontrealFoodViolations.Infrastructure/ # EF Core, sync, parsing CSV
tests/
└── MontrealFoodViolations.Tests/
```
