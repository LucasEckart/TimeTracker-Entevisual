using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker_Entevisual.Migrations
{
    /// <inheritdoc />
    public partial class AddDebeCambiarPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DebeCambiarPassword",
                table: "AspNetUsers",
                type: "bit",
                maxLength: 160,
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DebeCambiarPassword",
                table: "AspNetUsers");
        }
    }
}
