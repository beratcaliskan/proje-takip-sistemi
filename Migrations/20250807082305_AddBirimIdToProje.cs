using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjeTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddBirimIdToProje : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BirimId",
                table: "Projeler",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BirimId",
                table: "Projeler");
        }
    }
}
