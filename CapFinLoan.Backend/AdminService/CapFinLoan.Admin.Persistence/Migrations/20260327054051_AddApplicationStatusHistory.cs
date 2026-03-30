using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapFinLoan.Admin.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationStatusHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationStatusHistory",
                schema: "admin",
                columns: table => new
                {
                    HistoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdminUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: false),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationStatusHistory", x => x.HistoryId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationStatusHistory_AdminUserId",
                schema: "admin",
                table: "ApplicationStatusHistory",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationStatusHistory_ApplicationId",
                schema: "admin",
                table: "ApplicationStatusHistory",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationStatusHistory_ChangedAt",
                schema: "admin",
                table: "ApplicationStatusHistory",
                column: "ChangedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationStatusHistory",
                schema: "admin");
        }
    }
}
