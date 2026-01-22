using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker_Entevisual.Migrations
{
    /// <inheritdoc />
    public partial class MigracionInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Actividades_AspNetUsers_UsuarioId1",
                table: "Actividades");

            migrationBuilder.DropIndex(
                name: "IX_Actividades_UsuarioId1",
                table: "Actividades");

            migrationBuilder.DropColumn(
                name: "UsuarioId1",
                table: "Actividades");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "AspNetUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "UsuarioId",
                table: "Actividades",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_Actividades_UsuarioId",
                table: "Actividades",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Actividades_AspNetUsers_UsuarioId",
                table: "Actividades",
                column: "UsuarioId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Actividades_AspNetUsers_UsuarioId",
                table: "Actividades");

            migrationBuilder.DropIndex(
                name: "IX_Actividades_UsuarioId",
                table: "Actividades");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "AspNetUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UsuarioId",
                table: "Actividades",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "UsuarioId1",
                table: "Actividades",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Actividades_UsuarioId1",
                table: "Actividades",
                column: "UsuarioId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Actividades_AspNetUsers_UsuarioId1",
                table: "Actividades",
                column: "UsuarioId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
