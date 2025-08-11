using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjeTakip.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Birimler",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BirimAd = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Birimler", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Ilerlemeler",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    asama = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ilerlemeler", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Kullanicilar",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdSoyad = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Kimlik = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Rol = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kullanicilar", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Projeler",
                columns: table => new
                {
                    ProjeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjeAd = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Mudurluk = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Baskanlik = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Amac = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Kapsam = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Maliyet = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Ekip = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    bas = table.Column<DateTime>(type: "datetime2", nullable: true),
                    bit = table.Column<DateTime>(type: "datetime2", nullable: true),
                    olcut = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sponsor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Durum = table.Column<int>(type: "int", nullable: false),
                    personel = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projeler", x => x.ProjeID);
                });

            migrationBuilder.CreateTable(
                name: "Sponsorler",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BirimAd = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SponsorAd = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sponsorler", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "GanttAsamalari",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjeID = table.Column<int>(type: "int", nullable: false),
                    Asama = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Baslangic = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Bitis = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Gun = table.Column<int>(type: "int", nullable: false),
                    Sira = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanttAsamalari", x => x.id);
                    table.ForeignKey(
                        name: "FK_GanttAsamalari_Projeler_ProjeID",
                        column: x => x.ProjeID,
                        principalTable: "Projeler",
                        principalColumn: "ProjeID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GanttAsamalari_ProjeID",
                table: "GanttAsamalari",
                column: "ProjeID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Birimler");

            migrationBuilder.DropTable(
                name: "GanttAsamalari");

            migrationBuilder.DropTable(
                name: "Ilerlemeler");

            migrationBuilder.DropTable(
                name: "Kullanicilar");

            migrationBuilder.DropTable(
                name: "Sponsorler");

            migrationBuilder.DropTable(
                name: "Projeler");
        }
    }
}
