using MontrealFoodViolations.Domain.Entities;
using MontrealFoodViolations.Infrastructure.Models;
using MontrealFoodViolations.Infrastructure.Services;

namespace MontrealFoodViolations.Tests;

public class ViolationSyncServiceTests
{
    [Fact]
    public void Parse_Should_Ignore_Records_Without_IdPoursuite()
    {
        const string csv = "id_poursuite,business_id,date,description,adresse,date_jugement,etablissement,montant,proprietaire,ville,statut,date_statut,categorie\n" +
                           "123,456,20240115,Test description,1 Rue Test,20240515,Restaurant A,150.00,Proprietaire A,Montréal,Ouvert,20240520,Cuisine\n" +
                           ",789,20240116,Missing id,2 Rue Test,20240516,Restaurant B,200.00,Proprietaire B,Montréal,Ouvert,20240521,Cuisine\n";

        var violations = ViolationCsvParser.Parse(System.Text.Encoding.UTF8.GetBytes(csv));

        Assert.Single(violations);
        Assert.Equal(123L, violations[0].IdPoursuite);
    }

    [Fact]
    public void AreEquivalent_Should_Return_False_When_Values_Change()
    {
        var existing = new ExistingViolationSnapshot
        {
            IdPoursuite = 123,
            Description = "Original description",
            BusinessId = 456,
            Statut = "Ouvert"
        };

        var updated = new Violation
        {
            IdPoursuite = 123,
            Description = "Updated description",
            BusinessId = 456,
            Statut = "Fermé"
        };

        Assert.False(ViolationSnapshotComparer.AreEquivalent(existing, updated));
    }
}
