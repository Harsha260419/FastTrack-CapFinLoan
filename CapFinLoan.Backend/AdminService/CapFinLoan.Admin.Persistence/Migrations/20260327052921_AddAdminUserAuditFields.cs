using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapFinLoan.Admin.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminUserAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "admin",
                table: "Decisions",
                newName: "DecidedAt");

            migrationBuilder.AddColumn<Guid>(
                name: "AdminUserId",
                schema: "admin",
                table: "Decisions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_AdminUserId",
                schema: "admin",
                table: "Decisions",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_DecidedAt",
                schema: "admin",
                table: "Decisions",
                column: "DecidedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Decisions_AdminUserId",
                schema: "admin",
                table: "Decisions");

            migrationBuilder.DropIndex(
                name: "IX_Decisions_DecidedAt",
                schema: "admin",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "AdminUserId",
                schema: "admin",
                table: "Decisions");

            migrationBuilder.RenameColumn(
                name: "DecidedAt",
                schema: "admin",
                table: "Decisions",
                newName: "CreatedAt");
        }
    }
}
