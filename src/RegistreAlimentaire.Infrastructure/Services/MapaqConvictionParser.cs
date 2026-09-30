using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using RegistreAlimentaire.Domain.Entities;

namespace RegistreAlimentaire.Infrastructure.Services;

public static partial class MapaqConvictionParser
{
    public const string SourceName = "Mapaq";

    private static readonly string[] MultiWordCities = ["THETFORD MINES", "ACTON VALE"];

    private static readonly HashSet<string> MontrealAgglomeration = new(StringComparer.OrdinalIgnoreCase)
    {
        "MONTREAL",
        "WESTMOUNT",
        "OUTREMONT",
        "HAMPSTEAD",
        "COTE-SAINT-LUC",
        "MONTREAL-OUEST",
        "MONT-ROYAL",
        "DORVAL",
        "POINTE-CLAIRE",
        "KIRKLAND",
        "BEACONSFIELD",
        "BAIE-D'URFE",
        "BAIE-DURFE",
        "STE-ANNE-DE-BELLEVUE",
        "SAINTE-ANNE-DE-BELLEVUE",
        "SENNEVILLE",
        "DOLLARD-DES-ORMEAUX",
        "ILE-DORVAL",
        "L'ILE-DORVAL",
        "MONTREAL-EST"
    };

    private static readonly Dictionary<string, string> AccentedTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        ["quebec"] = "Québec",
        ["montreal"] = "Montréal",
        ["riviere"] = "Rivière",
        ["rivieres"] = "Rivières",
        ["ile"] = "Île",
        ["iles"] = "Îles",
        ["jerome"] = "Jérôme",
        ["therese"] = "Thérèse",
        ["felicien"] = "Félicien",
        ["cesaire"] = "Césaire",
        ["etienne"] = "Étienne",
        ["edouard"] = "Édouard",
        ["beloeil"] = "Belœil",
        ["levis"] = "Lévis"
    };

    public static List<Violation> Parse(byte[] csvBytes)
    {
        using var stream = new MemoryStream(csvBytes);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null,
            IgnoreBlankLines = true,
            Delimiter = ",",
            Mode = CsvMode.RFC4180,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim
        });

        var records = new List<Violation>();
        var seen = new HashSet<long>();
        foreach (var row in csv.GetRecords<MapaqRow>())
        {
            if (!string.Equals(row.SocCdLoi?.Trim(), "P-29", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var address = NullIfBlank(row.Adresse);
            var cityToken = ExtractCityToken(address);
            if (cityToken is null || MontrealAgglomeration.Contains(cityToken))
            {
                continue;
            }

            var etablissement = NullIfBlank(row.RaisonSociale) ?? NullIfBlank(row.NomExploitant);
            var description = NullIfBlank(row.Description) ?? string.Empty;
            if (etablissement is null || description.Length == 0)
            {
                continue;
            }

            var violation = new Violation
            {
                Source = SourceName,
                Etablissement = etablissement,
                Proprietaire = NullIfBlank(row.NomExploitant),
                Description = description,
                Adresse = address,
                Ville = ToDisplayName(cityToken),
                Categorie = ToDisplayName(row.TypeEtablissement),
                Date = ParseDate(row.DateInfraction),
                DateJugement = ParseDate(row.DateJugement),
                Montant = ParseAmount(row.Amende),
                Statut = ClosedStatus(row.InformationEtablissement)
            };
            violation.IdPoursuite = CreateStableId(violation);

            if (violation.IdPoursuite >= 0 || !seen.Add(violation.IdPoursuite))
            {
                continue;
            }

            records.Add(violation);
        }

        return records;
    }

    internal static string? ExtractCityToken(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        var left = address.Split(", (QC)", 2, StringSplitOptions.TrimEntries)[0].ToUpperInvariant();
        foreach (var city in MultiWordCities)
        {
            if (left.EndsWith(city, StringComparison.Ordinal))
            {
                return city;
            }
        }

        var parts = left.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? null : parts[^1];
    }

    internal static string? ToDisplayName(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return TokenPattern().Replace(raw.ToLowerInvariant(), match => Accent(Title(match.Value)));
    }

    internal static long CreateStableId(Violation violation)
    {
        var key = string.Join('|',
            SourceName,
            violation.Etablissement,
            violation.Adresse,
            violation.Description,
            violation.Date?.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            violation.Montant?.ToString(CultureInfo.InvariantCulture));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        var value = BitConverter.ToInt64(hash, 0);
        if (value is 0 or long.MinValue)
        {
            return -1;
        }

        return value < 0 ? value : -value;
    }

    private static string Title(string value) => char.ToUpperInvariant(value[0]) + value[1..];

    private static string Accent(string titled)
    {
        return AccentedTokens.TryGetValue(titled, out var accented) ? accented : titled;
    }

    private static string? ClosedStatus(string? information)
    {
        if (string.IsNullOrWhiteSpace(information))
        {
            return null;
        }

        return information.Contains("cessé", StringComparison.OrdinalIgnoreCase)
            ? "Fermé"
            : null;
    }

    private static DateOnly? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        string[] formats = ["MM/dd/yyyy HH:mm:ss", "MM/dd/yyyy", "yyyy-MM-dd"];
        if (DateTime.TryParseExact(trimmed, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
        {
            return DateOnly.FromDateTime(dateTime);
        }

        return DateOnly.TryParseExact(trimmed, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
    }

    private static decimal? ParseAmount(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Replace("$", string.Empty).Replace(" ", string.Empty).Replace("\u00a0", string.Empty).Replace(",", ".");
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
            ? amount
            : null;
    }

    private static string? NullIfBlank(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [GeneratedRegex(@"[^- ]+")]
    private static partial Regex TokenPattern();

    private sealed class MapaqRow
    {
        [CsvHelper.Configuration.Attributes.Name("Nom_exploitant")]
        public string? NomExploitant { get; set; }

        [CsvHelper.Configuration.Attributes.Name("Raison_sociale")]
        public string? RaisonSociale { get; set; }

        [CsvHelper.Configuration.Attributes.Name("Description_infraction")]
        public string? Description { get; set; }

        [CsvHelper.Configuration.Attributes.Name("Adresse_lieu_infraction")]
        public string? Adresse { get; set; }

        [CsvHelper.Configuration.Attributes.Name("Type_etablissement")]
        public string? TypeEtablissement { get; set; }

        [CsvHelper.Configuration.Attributes.Name("Information_etablissement")]
        public string? InformationEtablissement { get; set; }

        [CsvHelper.Configuration.Attributes.Name("Date_infraction")]
        public string? DateInfraction { get; set; }

        [CsvHelper.Configuration.Attributes.Name("Date_jugement")]
        public string? DateJugement { get; set; }

        [CsvHelper.Configuration.Attributes.Name("Amende")]
        public string? Amende { get; set; }

        [CsvHelper.Configuration.Attributes.Name("SOC_CD_LOI")]
        public string? SocCdLoi { get; set; }
    }
}
