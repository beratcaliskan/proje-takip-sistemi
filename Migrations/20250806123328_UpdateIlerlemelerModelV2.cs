using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjeTakip.Migrations
{
    /// <inheritdoc />
    public partial class UpdateIlerlemelerModelV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Önce mevcut verileri temizle
            migrationBuilder.Sql("DELETE FROM Ilerlemeler");
            
            migrationBuilder.RenameColumn(
                name: "asama",
                table: "Ilerlemeler",
                newName: "IlerlemeTanimi");

            migrationBuilder.AddColumn<string>(
                name: "Aciklama",
                table: "Ilerlemeler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GanttID",
                table: "Ilerlemeler",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "IlerlemeTarihi",
                table: "Ilerlemeler",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "ProjeID",
                table: "Ilerlemeler",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TamamlanmaYuzdesi",
                table: "Ilerlemeler",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Ilerlemeler_GanttID",
                table: "Ilerlemeler",
                column: "GanttID");

            migrationBuilder.CreateIndex(
                name: "IX_Ilerlemeler_ProjeID",
                table: "Ilerlemeler",
                column: "ProjeID");

            migrationBuilder.AddForeignKey(
                name: "FK_Ilerlemeler_GanttAsamalari_GanttID",
                table: "Ilerlemeler",
                column: "GanttID",
                principalTable: "GanttAsamalari",
                principalColumn: "id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_Ilerlemeler_Projeler_ProjeID",
                table: "Ilerlemeler",
                column: "ProjeID",
                principalTable: "Projeler",
                principalColumn: "ProjeID",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ilerlemeler_GanttAsamalari_GanttID",
                table: "Ilerlemeler");

            migrationBuilder.DropForeignKey(
                name: "FK_Ilerlemeler_Projeler_ProjeID",
                table: "Ilerlemeler");

            migrationBuilder.DropIndex(
                name: "IX_Ilerlemeler_GanttID",
                table: "Ilerlemeler");

            migrationBuilder.DropIndex(
                name: "IX_Ilerlemeler_ProjeID",
                table: "Ilerlemeler");

            migrationBuilder.DropColumn(
                name: "Aciklama",
                table: "Ilerlemeler");

            migrationBuilder.DropColumn(
                name: "GanttID",
                table: "Ilerlemeler");

            migrationBuilder.DropColumn(
                name: "IlerlemeTarihi",
                table: "Ilerlemeler");

            migrationBuilder.DropColumn(
                name: "ProjeID",
                table: "Ilerlemeler");

            migrationBuilder.DropColumn(
                name: "TamamlanmaYuzdesi",
                table: "Ilerlemeler");

            migrationBuilder.RenameColumn(
                name: "IlerlemeTanimi",
                table: "Ilerlemeler",
                newName: "asama");
        }
    }
}
