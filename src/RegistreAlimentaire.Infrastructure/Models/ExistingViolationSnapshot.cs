namespace RegistreAlimentaire.Infrastructure.Models;

public sealed class ExistingViolationSnapshot
{
    public long IdPoursuite { get; set; }
    public string Description { get; set; } = string.Empty;
    public long? BusinessId { get; set; }
    public DateOnly? Date { get; set; }
    public string? Adresse { get; set; }
    public DateOnly? DateJugement { get; set; }
    public string? Etablissement { get; set; }
    public decimal? Montant { get; set; }
    public string? Proprietaire { get; set; }
    public string? Ville { get; set; }
    public string? Statut { get; set; }
    public DateOnly? DateStatut { get; set; }
    public string? Categorie { get; set; }
}
