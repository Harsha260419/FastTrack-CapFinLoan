using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapFinLoan.Admin.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConvertAdminStatusesToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DecisionStatus",
                schema: "admin",
                table: "Decisions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "ToStatus",
                schema: "admin",
                table: "ApplicationStatusHistory",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "FromStatus",
                schema: "admin",
                table: "ApplicationStatusHistory",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.Sql(@"
                UPDATE [admin].[Decisions]
                SET [DecisionStatus] = CASE [DecisionStatus]
                    WHEN '4' THEN 'UNDER_REVIEW'
                    WHEN '5' THEN 'APPROVED'
                    WHEN '6' THEN 'REJECTED'
                    WHEN 'UnderReview' THEN 'UNDER_REVIEW'
                    WHEN 'Approved' THEN 'APPROVED'
                    WHEN 'Rejected' THEN 'REJECTED'
                    ELSE UPPER([DecisionStatus])
                END;
            ");

            migrationBuilder.Sql(@"
                UPDATE [admin].[ApplicationStatusHistory]
                SET [FromStatus] = CASE [FromStatus]
                    WHEN '0' THEN 'DRAFT'
                    WHEN '1' THEN 'SUBMITTED'
                    WHEN '2' THEN 'DOCS_PENDING'
                    WHEN '3' THEN 'DOCS_VERIFIED'
                    WHEN '4' THEN 'UNDER_REVIEW'
                    WHEN '5' THEN 'APPROVED'
                    WHEN '6' THEN 'REJECTED'
                    WHEN '7' THEN 'CLOSED'
                    WHEN 'DocsPending' THEN 'DOCS_PENDING'
                    WHEN 'DocsVerified' THEN 'DOCS_VERIFIED'
                    WHEN 'UnderReview' THEN 'UNDER_REVIEW'
                    ELSE UPPER([FromStatus])
                END,
                [ToStatus] = CASE [ToStatus]
                    WHEN '0' THEN 'DRAFT'
                    WHEN '1' THEN 'SUBMITTED'
                    WHEN '2' THEN 'DOCS_PENDING'
                    WHEN '3' THEN 'DOCS_VERIFIED'
                    WHEN '4' THEN 'UNDER_REVIEW'
                    WHEN '5' THEN 'APPROVED'
                    WHEN '6' THEN 'REJECTED'
                    WHEN '7' THEN 'CLOSED'
                    WHEN 'DocsPending' THEN 'DOCS_PENDING'
                    WHEN 'DocsVerified' THEN 'DOCS_VERIFIED'
                    WHEN 'UnderReview' THEN 'UNDER_REVIEW'
                    ELSE UPPER([ToStatus])
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "DecisionStatus",
                schema: "admin",
                table: "Decisions",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "ToStatus",
                schema: "admin",
                table: "ApplicationStatusHistory",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "FromStatus",
                schema: "admin",
                table: "ApplicationStatusHistory",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }
    }
}
