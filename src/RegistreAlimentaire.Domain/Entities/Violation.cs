namespace RegistreAlimentaire.Domain.Entities;

public class Violation
{
    public long IdPoursuite { get; set; }
    public long? BusinessId { get; set; }
    public DateOnly? Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Adresse { get; set; }
    public DateOnly? DateJugement { get; set; }
    public string? Etablissement { get; set; }
    public decimal? Montant { get; set; }
    public string? Proprietaire { get; set; }
    public string? Ville { get; set; }
    public string? Statut { get; set; }
    public DateOnly? DateStatut { get; set; }
    public string? Categorie { get; set; }
    public string Source { get; set; } = "Montreal";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
