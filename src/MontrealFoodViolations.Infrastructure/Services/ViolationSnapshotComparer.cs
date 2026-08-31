using MontrealFoodViolations.Domain.Entities;
using MontrealFoodViolations.Infrastructure.Models;

namespace MontrealFoodViolations.Infrastructure.Services;

public static class ViolationSnapshotComparer
{
    public static bool AreEquivalent(ExistingViolationSnapshot existingRow, Violation violation)
    {
        return existingRow.IdPoursuite == violation.IdPoursuite
            && existingRow.BusinessId == violation.BusinessId
            && existingRow.Date == violation.Date
            && existingRow.Description == violation.Description
            && existingRow.Adresse == violation.Adresse
            && existingRow.DateJugement == violation.DateJugement
            && existingRow.Etablissement == violation.Etablissement
            && existingRow.Montant == violation.Montant
            && existingRow.Proprietaire == violation.Proprietaire
            && existingRow.Ville == violation.Ville
            && existingRow.Statut == violation.Statut
            && existingRow.DateStatut == violation.DateStatut
            && existingRow.Categorie == violation.Categorie;
    }
}
