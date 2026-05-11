using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sudoku.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "primary_login_provider",
                table: "users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "username_confirmed",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "display_name",
                table: "user_logins",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "user_logins",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "primary_login_provider",
                table: "users");

            migrationBuilder.DropColumn(
                name: "username_confirmed",
                table: "users");

            migrationBuilder.DropColumn(
                name: "display_name",
                table: "user_logins");

            migrationBuilder.DropColumn(
                name: "email",
                table: "user_logins");
        }
    }
}
