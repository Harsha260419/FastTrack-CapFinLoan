using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapFinLoan.Auth.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleAuthFieldsToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                schema: "auth",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "AuthProvider",
                schema: "auth",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GoogleId",
                schema: "auth",
                table: "Users",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthProvider",
                schema: "auth",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GoogleId",
                schema: "auth",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                schema: "auth",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
