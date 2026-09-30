using RegistreAlimentaire.Domain.Entities;
using RegistreAlimentaire.Infrastructure.Models;
using RegistreAlimentaire.Infrastructure.Services;

namespace RegistreAlimentaire.Tests;

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

    [Fact]
    public void ParseMapaq_Should_Keep_Laval_And_Drop_Montreal_And_Animal_Welfare()
    {
        const string csv =
            "Nom_exploitant,Raison_sociale,Description_infraction,Adresse_lieu_infraction,Type_etablissement,Information_etablissement,Date_infraction,Date_jugement,Date_publication,Amende,SOC_CD_LOI\n" +
            "BOULANGERIE X INC.,BOULANGERIE X,Locaux non propres,\"3868 BOUL. LEMAN LAVAL, (QC) H7E1A1\",BOULANGERIE,,07/07/2025 00:00:00,01/07/2026 00:00:00,01/12/2026 00:00:00,500 $,P-29\n" +
            "RESTO MONTREAL INC.,RESTO MONTREAL,Locaux non propres,\"1 RUE TEST MONTREAL, (QC) H2X1Y4\",RESTAURANT,,07/07/2025 00:00:00,01/07/2026 00:00:00,01/12/2026 00:00:00,250 $,P-29\n" +
            "FERME Y,FERME Y,Bien-etre animal,\"10 RANG 1 LEVIS, (QC) G6V1A1\",FERME,,07/07/2025 00:00:00,01/07/2026 00:00:00,01/12/2026 00:00:00,400 $,B-3.1\n" +
            "MINE Z INC.,MINE Z,Locaux non propres,\"68 RUE NOTRE-DAME OUEST THETFORD MINES, (QC) G6G1J4\",RESTAURANT,\"L'exploitant, qui opérait à la date de l'infraction, a cessé ses opérations\",06/01/2025 00:00:00,08/01/2025 00:00:00,08/02/2025 00:00:00,1 200 $,P-29\n";

        var violations = MapaqConvictionParser.Parse(System.Text.Encoding.UTF8.GetBytes(csv));

        Assert.Equal(2, violations.Count);
        var laval = Assert.Single(violations, violation => violation.Ville == "Laval");
        Assert.Equal("BOULANGERIE X", laval.Etablissement);
        Assert.Equal(new DateOnly(2025, 7, 7), laval.Date);
        Assert.Equal(500m, laval.Montant);
        Assert.Equal(MapaqConvictionParser.SourceName, laval.Source);
        Assert.True(laval.IdPoursuite < 0);

        var mines = Assert.Single(violations, violation => violation.Ville == "Thetford Mines");
        Assert.Equal("Fermé", mines.Statut);
        Assert.Equal(1200m, mines.Montant);

        var again = MapaqConvictionParser.Parse(System.Text.Encoding.UTF8.GetBytes(csv));
        Assert.Equal(laval.IdPoursuite, again[0].IdPoursuite);
    }
}
