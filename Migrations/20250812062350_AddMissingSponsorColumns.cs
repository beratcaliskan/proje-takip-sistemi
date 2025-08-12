using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjeTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingSponsorColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Aciklama",
                table: "Sponsorler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IletisimBilgisi",
                table: "Sponsorler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projeler_BirimId",
                table: "Projeler",
                column: "BirimId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projeler_Birimler_BirimId",
                table: "Projeler",
                column: "BirimId",
                principalTable: "Birimler",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projeler_Birimler_BirimId",
                table: "Projeler");

            migrationBuilder.DropIndex(
                name: "IX_Projeler_BirimId",
                table: "Projeler");

            migrationBuilder.DropColumn(
                name: "Aciklama",
                table: "Sponsorler");

            migrationBuilder.DropColumn(
                name: "IletisimBilgisi",
                table: "Sponsorler");
        }
    }
}
