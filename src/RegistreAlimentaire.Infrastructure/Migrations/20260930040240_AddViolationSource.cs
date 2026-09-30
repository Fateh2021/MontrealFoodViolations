using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RegistreAlimentaire.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddViolationSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Violations",
                type: "TEXT",
                maxLength: 40,
                nullable: false,
                defaultValue: "Montreal");

            migrationBuilder.CreateIndex(
                name: "IX_Violations_Source",
                table: "Violations",
                column: "Source");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Violations_Source",
                table: "Violations");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Violations");
        }
    }
}
