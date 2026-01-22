using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker_Entevisual.Migrations
{
    /// <inheritdoc />
    public partial class MarcaTiempo_Oculta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Oculta",
                table: "MarcasTiempo",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Oculta",
                table: "MarcasTiempo");
        }
    }
}
