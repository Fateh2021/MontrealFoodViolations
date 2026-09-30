# MontrealFoodViolations

MontrealFoodViolations est une application full-stack .NET 10 : une API ASP.NET Core qui télécharge, analyse et synchronise le jeu de données public des infractions alimentaires de Montréal dans SQLite, plus une interface Vue 3 pour la recherche, les statistiques et les fiches établissement.

## Objectif

L'application récupère le dernier CSV publié par la Ville de Montréal, met à jour SQLite sans dupliquer les dossiers, et expose les données par une API REST. L'API sert aussi une interface Vue 3 : un champ « Nom ou adresse », une recherche avancée, le tri, la pagination, l'export CSV et la fiche établissement. Sur un téléphone, les résultats sont des fiches. La synchronisation tourne en arrière-plan et peut être lancée à la main via l'API.

Interface locale : **http://localhost:5067** (après `dotnet run`)

Site déployé : **https://montrealfoodviolations-bsfrgnhtfygrduhu.canadaeast-01.azurewebsites.net**

## Source officielle

Le jeu de données vient directement de la Ville de Montréal :

https://data.montreal.ca/dataset/05a9e718-6810-4e73-8bb9-5955efeb91a0/resource/7f939a08-be8a-45e1-b208-d8744dca8fc6/download/violations.csv

Le CSV n'est pas versionné dans Git. L'application télécharge le fichier en direct à l'exécution.

Les condamnations hors Montréal viennent du MAPAQ, licence CC-BY 4.0 :

https://www.donneesquebec.ca/recherche/dataset/515374ee-ce34-464f-9875-7d1af3fa9b2a/resource/40105615-3abf-414b-bcba-182e8f2c5eb2/download/listecondamnation.csv

L'agglomération de Montréal reste sur le fichier de la Ville. Le fichier du MAPAQ couvre les autres municipalités, dont Laval et Longueuil. Il ne contient pas de statut Ouvert / Fermé, sauf lorsqu'il indique que l'exploitant a cessé ses opérations.

## Colonnes du fichier

L'en-tête réel contient ces colonnes :

- id_poursuite
- business_id
- date
- description
- adresse
- date_jugement
- etablissement
- montant
- proprietaire
- ville
- statut
- date_statut
- categorie

L'identité unique d'un dossier est le champ `id_poursuite`. C'est la clé utilisée pour les insertions, les mises à jour et la prévention des doublons.

## Interface web

- Au départ, les amendes par année couvrent toutes les villes. Le menu **Ville** filtre seulement la liste. Montréal limite la liste à l'agglomération. Les autres choix viennent du MAPAQ.
- Les années antérieures à 2024 ne contiennent que Montréal : le MAPAQ publie les 24 derniers mois.
- Un bouton soleil ou lune change le thème. Le choix est conservé dans le navigateur.
- Un champ **Nom ou adresse** lance la recherche. Les autres filtres sont rangés sous **Recherche avancée**.
- Le tableau commence par l'établissement, l'adresse, les dates, le montant et le statut. Les dates sont affichées en français, par exemple « 30 janvier 2025 ». Le statut Ouvert est vert et Fermé est rouge.
- Le tri par défaut va de la date la plus récente à la plus ancienne.
- Les identifiants techniques (`id_poursuite`, `business_id`) restent dans l'API, les clés internes et l'export CSV. Ils ne sont pas affichés dans le tableau ni dans la fiche établissement.
- En dessous de 720 px, le tableau est remplacé par des fiches. Toucher le nom ouvre l'historique de l'établissement.
- L'export CSV télécharge les résultats filtrés, jusqu'à 5 000 lignes.

## Architecture

```
frontend/                                  # Interface Vue 3 (Vite, Composition API)
src/
├── MontrealFoodViolations.Api/            # API REST + wwwroot (build de production)
├── MontrealFoodViolations.Application/    # contrats, modèles, options
├── MontrealFoodViolations.Domain/         # entités
└── MontrealFoodViolations.Infrastructure/ # EF Core, synchro CSV, client HTTP
tests/
└── MontrealFoodViolations.Tests/
```

- API web ASP.NET Core
- Vue 3 (Composition API) + Vite
- EF Core + SQLite
- BackgroundService pour la synchro planifiée
- HttpClient via IHttpClientFactory
- OpenAPI + Swagger UI (`/swagger`)
- Tests xUnit
- Injection de dépendances et journalisation

## Synchronisation en arrière-plan

Le planificateur est `ViolationSyncBackgroundService`. Il suit `ViolationSync:IntervalHours`. La valeur par défaut est 24 heures. Une synchronisation part dès le démarrage, puis se répète à chaque intervalle.

Le cycle est le suivant :

1. télécharger le dernier CSV
2. valider la réponse
3. analyser le CSV
4. comparer avec SQLite
5. insérer les nouvelles lignes
6. mettre à jour les lignes modifiées
7. journaliser le résultat
8. attendre le prochain intervalle

Si le téléchargement échoue, l'application journalise le problème et conserve les données déjà en base.

## Base de données

SQLite est configuré par la chaîne de connexion dans `appsettings.json`. Les migrations EF Core s'appliquent automatiquement au démarrage.

Les entités principales sont :

- `Violation`
- `DatasetSyncState`

Pour appliquer les migrations à la main (optionnel) :

```bash
dotnet ef database update --project src/MontrealFoodViolations.Api/MontrealFoodViolations.Api.csproj
```

## Endpoints de l'API

- `GET /api/violations?page=1&pageSize=25` — liste paginée
- `GET /api/violations/{id}` — détail d'une infraction
- `GET /api/violations/business/{businessId}` — fiche établissement
- `GET /api/violations/cities` — villes disponibles hors Montréal
- `GET /api/violations/search` — recherche filtrée, avec `ville`
- `GET /api/violations/export` — export CSV
- `GET /api/violations/stats` — statistiques des amendes
- `POST /api/sync` — synchronisation manuelle
- `GET /api/sync/status` — état de la dernière synchronisation

## Installation

1. Installer le SDK .NET (le projet cible .NET 10).
2. Node.js n'est nécessaire que pour modifier l'interface Vue.
3. Restaurer les paquets : `dotnet restore`
4. Lancer l'API (le schéma est créé au démarrage).

## Exécution

```bash
dotnet run --project src/MontrealFoodViolations.Api/MontrealFoodViolations.Api.csproj
```

Ouvrir **http://localhost:5067**. Swagger est sur **`/swagger`**, en local comme sur le site déployé. ASP.NET sert le build Vue de production depuis `wwwroot`. Après une modification dans `frontend/` :

```bash
cd frontend
npm install
npm run build
```

Pour développer seulement l'interface, lancer l'API puis `npm run dev` dans `frontend/` (Vite proxifie `/api` vers `http://localhost:5067`).

## Tests

```bash
dotnet test
```

## Configuration

```json
{
  "ViolationSync": {
    "Enabled": true,
    "IntervalHours": 24
  },
  "MontrealDataset": {
    "ViolationsUrl": "https://data.montreal.ca/dataset/05a9e718-6810-4e73-8bb9-5955efeb91a0/resource/7f939a08-be8a-45e1-b208-d8744dca8fc6/download/violations.csv"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=montrealfoodviolations.db"
  }
}
```

## Stratégie de synchronisation

La synchronisation est idempotente. Si le même fichier est téléchargé à nouveau, les lignes inchangées sont reconnues et aucun doublon n'est créé. Les nouvelles lignes sont insérées, les lignes modifiées sont mises à jour, et aucune donnée n'est supprimée.

## Hébergement

L'API et l'interface sont déployées sur Azure App Service (offre gratuite F1), depuis la branche `master`. Le workflow GitHub Actions publie le projet .NET. Le build Vue de `wwwroot` est déjà dans le dépôt, donc le pipeline ne lance pas `npm`.

Les profils de publication Azure, les fichiers `.env`, `secrets.json` et les certificats sont ignorés par Git. Le mot de passe de déploiement reste dans un secret GitHub.

## Notes

Le projet reste structuré pour pouvoir changer de base de données plus tard. SQLite est la base utilisée en local et sur Azure.

## Développement

Le projet a été conçu et architecturé par l'auteur. Des outils d'IA (Cursor) ont accéléré le code répétitif, la documentation, les itérations d'interface et les suggestions de refactoring.

Décisions prises à la main :

- architecture en couches (Domain / Application / Infrastructure / API)
- synchronisation idempotente fondée sur `id_poursuite`
- modèle de données aligné sur le jeu de données ouvert de Montréal
- conception des endpoints et de l'expérience de recherche

Le guide complet de l'API, en français, est dans [DOCUMENTATION.md](DOCUMENTATION.md).
