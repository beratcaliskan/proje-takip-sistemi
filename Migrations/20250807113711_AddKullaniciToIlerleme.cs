using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjeTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddKullaniciToIlerleme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KullaniciID",
                table: "Ilerlemeler",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ilerlemeler_KullaniciID",
                table: "Ilerlemeler",
                column: "KullaniciID");

            migrationBuilder.AddForeignKey(
                name: "FK_Ilerlemeler_Kullanicilar_KullaniciID",
                table: "Ilerlemeler",
                column: "KullaniciID",
                principalTable: "Kullanicilar",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ilerlemeler_Kullanicilar_KullaniciID",
                table: "Ilerlemeler");

            migrationBuilder.DropIndex(
                name: "IX_Ilerlemeler_KullaniciID",
                table: "Ilerlemeler");

            migrationBuilder.DropColumn(
                name: "KullaniciID",
                table: "Ilerlemeler");
        }
    }
}
