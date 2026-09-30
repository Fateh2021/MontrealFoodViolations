using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RegistreAlimentaire.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DatasetSyncStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DatasetName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    LastSyncStartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastSyncCompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastHash = table.Column<string>(type: "TEXT", nullable: true),
                    LastETag = table.Column<string>(type: "TEXT", nullable: true),
                    LastModified = table.Column<string>(type: "TEXT", nullable: true),
                    LastRowCount = table.Column<int>(type: "INTEGER", nullable: true),
                    LastInsertedRows = table.Column<int>(type: "INTEGER", nullable: false),
                    LastUpdatedRows = table.Column<int>(type: "INTEGER", nullable: false),
                    LastUnchangedRows = table.Column<int>(type: "INTEGER", nullable: false),
                    LastSkippedRows = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LastError = table.Column<string>(type: "TEXT", nullable: true),
                    LastSuccessAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetSyncStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Violations",
                columns: table => new
                {
                    IdPoursuite = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BusinessId = table.Column<long>(type: "INTEGER", nullable: true),
                    Date = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Adresse = table.Column<string>(type: "TEXT", nullable: true),
                    DateJugement = table.Column<string>(type: "TEXT", nullable: true),
                    Etablissement = table.Column<string>(type: "TEXT", nullable: true),
                    Montant = table.Column<decimal>(type: "TEXT", nullable: true),
                    Proprietaire = table.Column<string>(type: "TEXT", nullable: true),
                    Ville = table.Column<string>(type: "TEXT", nullable: true),
                    Statut = table.Column<string>(type: "TEXT", nullable: true),
                    DateStatut = table.Column<string>(type: "TEXT", nullable: true),
                    Categorie = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Violations", x => x.IdPoursuite);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DatasetSyncStates_DatasetName",
                table: "DatasetSyncStates",
                column: "DatasetName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Violations_BusinessId",
                table: "Violations",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Violations_Categorie",
                table: "Violations",
                column: "Categorie");

            migrationBuilder.CreateIndex(
                name: "IX_Violations_Date",
                table: "Violations",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Violations_Etablissement",
                table: "Violations",
                column: "Etablissement");

            migrationBuilder.CreateIndex(
                name: "IX_Violations_Statut",
                table: "Violations",
                column: "Statut");

            migrationBuilder.CreateIndex(
                name: "IX_Violations_Ville",
                table: "Violations",
                column: "Ville");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DatasetSyncStates");

            migrationBuilder.DropTable(
                name: "Violations");
        }
    }
}
