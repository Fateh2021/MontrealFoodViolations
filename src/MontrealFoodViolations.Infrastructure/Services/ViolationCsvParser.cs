using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using MontrealFoodViolations.Domain.Entities;

namespace MontrealFoodViolations.Infrastructure.Services;

public static class ViolationCsvParser
{
    public static List<Violation> Parse(byte[] csvBytes)
    {
        using var stream = new MemoryStream(csvBytes);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            IgnoreBlankLines = true,
            Delimiter = ",",
            Mode = CsvMode.RFC4180,
            BadDataFound = null,
            DetectColumnCountChanges = true,
            TrimOptions = TrimOptions.Trim,
            ShouldSkipRecord = _ => false
        });

        var records = new List<Violation>();
        csv.Read();
        csv.ReadHeader();
        var header = csv.HeaderRecord ?? Array.Empty<string>();

        while (csv.Read())
        {
            var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in header)
            {
                values[key] = csv.GetField(key);
            }

            var violation = new Violation
            {
                IdPoursuite = ParseLong(values.GetValueOrDefault("id_poursuite")),
                BusinessId = ParseLongNullable(values.GetValueOrDefault("business_id")),
                Date = ParseDate(values.GetValueOrDefault("date")),
                Description = values.GetValueOrDefault("description") ?? string.Empty,
                Adresse = values.GetValueOrDefault("adresse"),
                DateJugement = ParseDate(values.GetValueOrDefault("date_jugement")),
                Etablissement = values.GetValueOrDefault("etablissement"),
                Montant = ParseDecimalNullable(values.GetValueOrDefault("montant")),
                Proprietaire = values.GetValueOrDefault("proprietaire"),
                Ville = values.GetValueOrDefault("ville"),
                Statut = values.GetValueOrDefault("statut"),
                DateStatut = ParseDate(values.GetValueOrDefault("date_statut")),
                Categorie = values.GetValueOrDefault("categorie")
            };

            if (violation.IdPoursuite <= 0)
            {
                continue;
            }

            records.Add(violation);
        }

        return records;
    }

    private static long ParseLong(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        return long.TryParse(value.Trim(), out var parsed) ? parsed : 0;
    }

    private static long? ParseLongNullable(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return long.TryParse(value.Trim(), out var parsed) ? parsed : null;
    }

    private static DateOnly? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();

        if (trimmed.Length == 8 && DateOnly.TryParseExact(trimmed, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        return DateOnly.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate)
            ? parsedDate
            : null;
    }

    private static decimal? ParseDecimalNullable(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Replace("$", string.Empty).Replace(" ", string.Empty).Replace(",", string.Empty);
        return decimal.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }
}
