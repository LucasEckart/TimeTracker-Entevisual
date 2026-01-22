using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker_Entevisual.Migrations
{
    /// <inheritdoc />
    public partial class Tiempo_OcultoEnNotas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "OcultoEnNotas",
                table: "Tiempos",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OcultoEnNotas",
                table: "Tiempos");
        }
    }
}
