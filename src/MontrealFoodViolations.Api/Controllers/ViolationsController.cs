using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MontrealFoodViolations.Domain.Entities;
using MontrealFoodViolations.Infrastructure.Data;

namespace MontrealFoodViolations.Api.Controllers;

[ApiController]
[Route("api/violations")]
public class ViolationsController : ControllerBase
{
    private readonly MontrealFoodViolationsDbContext _dbContext;

    public ViolationsController(MontrealFoodViolationsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    [EndpointSummary("Liste paginée des infractions.")]
    public async Task<IActionResult> Get(int page = 1, int pageSize = 25, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 25;

        var query = _dbContext.Violations.AsNoTracking().OrderBy(x => x.IdPoursuite);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return Ok(new
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            Items = items
        });
    }

    [HttpGet("business/{businessId:long}")]
    [EndpointSummary("Fiche d'un établissement et historique de ses infractions.")]
    public async Task<IActionResult> GetByBusinessId(long businessId, CancellationToken cancellationToken)
    {
        var violations = await _dbContext.Violations.AsNoTracking()
            .Where(x => x.BusinessId == businessId)
            .OrderByDescending(x => x.DateJugement)
            .ThenByDescending(x => x.IdPoursuite)
            .ToListAsync(cancellationToken);

        if (violations.Count == 0)
        {
            return NotFound();
        }

        var latest = violations[0];
        var totalFines = violations.Where(x => x.Montant > 0).Sum(x => x.Montant ?? 0m);

        return Ok(new
        {
            BusinessId = businessId,
            latest.Etablissement,
            latest.Adresse,
            latest.Ville,
            latest.Proprietaire,
            latest.Statut,
            ViolationCount = violations.Count,
            TotalFines = totalFines,
            AverageFine = violations.Count == 0 ? 0m : totalFines / violations.Count,
            Violations = violations
        });
    }

    [HttpGet("{id:long}")]
    [EndpointSummary("Détail d'une infraction à partir de son id_poursuite.")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var violation = await _dbContext.Violations.AsNoTracking().FirstOrDefaultAsync(x => x.IdPoursuite == id, cancellationToken);
        if (violation is null) return NotFound();
        return Ok(violation);
    }

    [HttpGet("cities")]
    [EndpointSummary("Villes du MAPAQ, hors agglomération de Montréal.")]
    public async Task<IActionResult> Cities(CancellationToken cancellationToken)
    {
        var cities = await _dbContext.Violations.AsNoTracking()
            .Where(violation => violation.Source == "Mapaq" && violation.Ville != null && violation.Ville != "")
            .Select(violation => violation.Ville!)
            .Distinct()
            .OrderBy(city => city)
            .ToListAsync(cancellationToken);

        return Ok(new { cities });
    }

    [HttpGet("search")]
    [EndpointSummary("Recherche filtrée, avec tri et pagination. ville=Montréal limite à l'agglomération.")]
    public async Task<IActionResult> Search(
        [FromQuery] string? search,
        [FromQuery] string? etablissement,
        [FromQuery] string? adresse,
        [FromQuery] string? categorie,
        [FromQuery] string? statut,
        [FromQuery] string? proprietaire,
        [FromQuery] string? description,
        [FromQuery] string? ville,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string sortBy = "IdPoursuite",
        [FromQuery] bool descending = false,
        CancellationToken cancellationToken = default)
    {
        var dbQuery = ApplyVille(
            ApplySearchFilters(_dbContext.Violations.AsNoTracking(), search, etablissement, adresse, categorie, statut, proprietaire, description),
            ville);
        var orderedQuery = ApplySort(dbQuery, sortBy, descending);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var total = await orderedQuery.CountAsync(cancellationToken);
        var items = await orderedQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return Ok(new
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            Items = items
        });
    }

    [HttpGet("export")]
    [EndpointSummary("Export CSV des résultats filtrés (5 000 lignes maximum).")]
    public async Task<IActionResult> Export(
        [FromQuery] string? search,
        [FromQuery] string? etablissement,
        [FromQuery] string? adresse,
        [FromQuery] string? categorie,
        [FromQuery] string? statut,
        [FromQuery] string? proprietaire,
        [FromQuery] string? description,
        [FromQuery] string? ville,
        [FromQuery] string sortBy = "IdPoursuite",
        [FromQuery] bool descending = false,
        CancellationToken cancellationToken = default)
    {
        const int maxRows = 5000;
        var dbQuery = ApplyVille(
            ApplySearchFilters(_dbContext.Violations.AsNoTracking(), search, etablissement, adresse, categorie, statut, proprietaire, description),
            ville);
        var orderedQuery = ApplySort(dbQuery, sortBy, descending);

        var items = await orderedQuery.Take(maxRows).ToListAsync(cancellationToken);
        var csv = "\uFEFF" + BuildCsv(items);
        var fileName = $"condamnations-alimentaires-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";

        return File(Encoding.UTF8.GetBytes(csv), "text/csv; charset=utf-8", fileName);
    }

    [HttpGet("stats")]
    [EndpointSummary("Totaux et statistiques des amendes, pour une ville ou pour Montréal.")]
    public async Task<IActionResult> Stats([FromQuery] string? ville, CancellationToken cancellationToken)
    {
        var violations = ApplyVille(_dbContext.Violations.AsNoTracking(), ville);
        var total = await violations.CountAsync(cancellationToken);
        var byCity = await violations.Where(x => x.Ville != null).GroupBy(x => x.Ville).Select(g => new { City = g.Key, Count = g.Count() }).OrderByDescending(x => x.Count).Take(10).ToListAsync(cancellationToken);
        var byCategory = await violations.Where(x => x.Categorie != null).GroupBy(x => x.Categorie).Select(g => new { Category = g.Key, Count = g.Count() }).OrderByDescending(x => x.Count).Take(10).ToListAsync(cancellationToken);

        var fineRecords = await violations
            .Where(x => x.Montant != null)
            .Select(x => new { x.Montant, x.DateJugement, x.Categorie, x.Ville, x.Source })
            .ToListAsync(cancellationToken);

        var fined = fineRecords.Where(x => x.Montant > 0).ToList();
        var finesCount = fined.Count;
        var totalFinesAmount = fined.Sum(x => x.Montant!.Value);
        var averageFine = finesCount == 0 ? 0m : totalFinesAmount / finesCount;
        var maxFine = finesCount == 0 ? 0m : fined.Max(x => x.Montant!.Value);

        var finesByYear = fined
            .Where(x => x.DateJugement != null)
            .GroupBy(x => x.DateJugement!.Value.Year)
            .Select(g => new
            {
                Year = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(x => x.Montant!.Value)
            })
            .OrderByDescending(x => x.Year)
            .Take(8)
            .ToList();

        var topCategoriesByFines = fined
            .Where(x => x.Categorie != null)
            .GroupBy(x => x.Categorie)
            .Select(g => new
            {
                Category = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(x => x.Montant!.Value)
            })
            .OrderByDescending(x => x.TotalAmount)
            .Take(5)
            .ToList();

        var finesByCity = fined
            .Select(x => new
            {
                x.Montant,
                Territory = IncludesEveryCity(ville) && x.Source == "Montreal"
                    ? "Montréal"
                    : x.Ville
            })
            .Where(x => x.Territory != null)
            .GroupBy(x => x.Territory!)
            .Select(g => new
            {
                City = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(x => x.Montant!.Value)
            })
            .OrderByDescending(x => x.TotalAmount)
            .Take(8)
            .ToList();

        return Ok(new
        {
            TotalViolations = total,
            ByCity = byCity,
            ByCategory = byCategory,
            Fines = new
            {
                Count = finesCount,
                TotalAmount = totalFinesAmount,
                AverageAmount = averageFine,
                MaxAmount = maxFine
            },
            FinesByYear = finesByYear,
            TopCategoriesByFines = topCategoriesByFines,
            FinesByCity = finesByCity
        });
    }

    private static IQueryable<Violation> ApplySearchFilters(
        IQueryable<Violation> dbQuery,
        string? search,
        string? etablissement,
        string? adresse,
        string? categorie,
        string? statut,
        string? proprietaire,
        string? description)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedLower = search.Trim().ToLower();
            dbQuery = dbQuery.Where(x =>
                (x.Description != null && x.Description.ToLower().Contains(normalizedLower)) ||
                (x.Etablissement != null && x.Etablissement.ToLower().Contains(normalizedLower)) ||
                (x.Adresse != null && x.Adresse.ToLower().Contains(normalizedLower)) ||
                (x.Categorie != null && x.Categorie.ToLower().Contains(normalizedLower)) ||
                (x.Proprietaire != null && x.Proprietaire.ToLower().Contains(normalizedLower)) ||
                (x.Statut != null && x.Statut.ToLower().Contains(normalizedLower)));
        }

        if (!string.IsNullOrWhiteSpace(etablissement))
        {
            var normalizedLower = etablissement.Trim().ToLower();
            dbQuery = dbQuery.Where(x => x.Etablissement != null && x.Etablissement.ToLower().Contains(normalizedLower));
        }

        if (!string.IsNullOrWhiteSpace(adresse))
        {
            var normalizedLower = adresse.Trim().ToLower();
            dbQuery = dbQuery.Where(x => x.Adresse != null && x.Adresse.ToLower().Contains(normalizedLower));
        }

        if (!string.IsNullOrWhiteSpace(categorie))
        {
            var normalizedLower = categorie.Trim().ToLower();
            dbQuery = dbQuery.Where(x => x.Categorie != null && x.Categorie.ToLower().Contains(normalizedLower));
        }

        if (!string.IsNullOrWhiteSpace(statut))
        {
            var normalizedLower = statut.Trim().ToLower();
            dbQuery = dbQuery.Where(x => x.Statut != null && x.Statut.ToLower().Contains(normalizedLower));
        }

        if (!string.IsNullOrWhiteSpace(proprietaire))
        {
            var normalizedLower = proprietaire.Trim().ToLower();
            dbQuery = dbQuery.Where(x => x.Proprietaire != null && x.Proprietaire.ToLower().Contains(normalizedLower));
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            var normalizedLower = description.Trim().ToLower();
            dbQuery = dbQuery.Where(x => x.Description != null && x.Description.ToLower().Contains(normalizedLower));
        }

        return dbQuery;
    }

    private static bool IncludesEveryCity(string? ville)
    {
        return string.IsNullOrWhiteSpace(ville)
            || ville.Equals("toutes", StringComparison.OrdinalIgnoreCase)
            || ville.Equals("toutes les villes", StringComparison.OrdinalIgnoreCase);
    }

    private static IQueryable<Violation> ApplyVille(IQueryable<Violation> dbQuery, string? ville)
    {
        if (IncludesEveryCity(ville))
        {
            return dbQuery;
        }

        var selected = ville!.Trim();
        if (selected.Equals("montréal", StringComparison.OrdinalIgnoreCase)
            || selected.Equals("montreal", StringComparison.OrdinalIgnoreCase))
        {
            return dbQuery.Where(violation => violation.Source == "Montreal");
        }

        var normalized = selected.ToLower();
        return dbQuery.Where(violation => violation.Ville != null && violation.Ville.ToLower() == normalized);
    }

    private static IQueryable<Violation> ApplySort(IQueryable<Violation> dbQuery, string sortBy, bool descending)
    {
        return sortBy.ToLowerInvariant() switch
        {
            "idpoursuite" => descending ? dbQuery.OrderByDescending(x => x.IdPoursuite) : dbQuery.OrderBy(x => x.IdPoursuite),
            "businessid" => descending ? dbQuery.OrderByDescending(x => x.BusinessId) : dbQuery.OrderBy(x => x.BusinessId),
            "etablissement" => descending ? dbQuery.OrderByDescending(x => x.Etablissement) : dbQuery.OrderBy(x => x.Etablissement),
            "adresse" => descending ? dbQuery.OrderByDescending(x => x.Adresse) : dbQuery.OrderBy(x => x.Adresse),
            "ville" => descending ? dbQuery.OrderByDescending(x => x.Ville) : dbQuery.OrderBy(x => x.Ville),
            "categorie" => descending ? dbQuery.OrderByDescending(x => x.Categorie) : dbQuery.OrderBy(x => x.Categorie),
            "statut" => descending ? dbQuery.OrderByDescending(x => x.Statut) : dbQuery.OrderBy(x => x.Statut),
            "proprietaire" => descending ? dbQuery.OrderByDescending(x => x.Proprietaire) : dbQuery.OrderBy(x => x.Proprietaire),
            "montant" => descending ? dbQuery.OrderByDescending(x => x.Montant) : dbQuery.OrderBy(x => x.Montant),
            "description" => descending ? dbQuery.OrderByDescending(x => x.Description) : dbQuery.OrderBy(x => x.Description),
            "date" => descending ? dbQuery.OrderByDescending(x => x.Date) : dbQuery.OrderBy(x => x.Date),
            "datejugement" => descending ? dbQuery.OrderByDescending(x => x.DateJugement) : dbQuery.OrderBy(x => x.DateJugement),
            _ => descending ? dbQuery.OrderByDescending(x => x.IdPoursuite) : dbQuery.OrderBy(x => x.IdPoursuite)
        };
    }

    private static string BuildCsv(IReadOnlyList<Violation> items)
    {
        static string Escape(string? value)
        {
            value ??= string.Empty;
            if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }

            return value;
        }

        static string FormatDate(DateOnly? value) => value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;

        var builder = new StringBuilder();
        builder.AppendLine("id_poursuite,business_id,etablissement,adresse,ville,categorie,statut,proprietaire,date,date_jugement,montant,description");

        foreach (var item in items)
        {
            builder.Append(item.IdPoursuite).Append(',');
            builder.Append(item.BusinessId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty).Append(',');
            builder.Append(Escape(item.Etablissement)).Append(',');
            builder.Append(Escape(item.Adresse)).Append(',');
            builder.Append(Escape(item.Ville)).Append(',');
            builder.Append(Escape(item.Categorie)).Append(',');
            builder.Append(Escape(item.Statut)).Append(',');
            builder.Append(Escape(item.Proprietaire)).Append(',');
            builder.Append(FormatDate(item.Date)).Append(',');
            builder.Append(FormatDate(item.DateJugement)).Append(',');
            builder.Append(item.Montant?.ToString(CultureInfo.InvariantCulture) ?? string.Empty).Append(',');
            builder.Append(Escape(item.Description));
            builder.AppendLine();
        }

        return builder.ToString();
    }
}
