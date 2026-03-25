using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapFinLoan.Application.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanWizardFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddressLine1",
                schema: "core",
                table: "LoanApplications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AddressLine2",
                schema: "core",
                table: "LoanApplications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "AnnualIncome",
                schema: "core",
                table: "LoanApplications",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "City",
                schema: "core",
                table: "LoanApplications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                schema: "core",
                table: "LoanApplications",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "EmployerName",
                schema: "core",
                table: "LoanApplications",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmploymentType",
                schema: "core",
                table: "LoanApplications",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ExistingEmiAmount",
                schema: "core",
                table: "LoanApplications",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                schema: "core",
                table: "LoanApplications",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                schema: "core",
                table: "LoanApplications",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                schema: "core",
                table: "LoanApplications",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyIncome",
                schema: "core",
                table: "LoanApplications",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                schema: "core",
                table: "LoanApplications",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "State",
                schema: "core",
                table: "LoanApplications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddressLine1",
                schema: "core",
                table: "LoanApplications");

            migrationBuilder.DropColumn(
                name: "AddressLine2",
                schema: "core",
                table: "LoanApplications");

            migrationBuilder.DropColumn(
                name: "AnnualIncome",
                schema: "core",
                table: "LoanApplications");

            migrationBuilder.DropColumn(
                name: "City",
                schema: "core",
                table: "LoanApplications");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                schema: "core",
                table: "LoanApplications");

            migrationBuilder.DropColumn(
                name: "EmployerName",
                schema: "core",
                table: "LoanApplications");

            migrationBuilder.DropColumn(
                name: "EmploymentType",
                schema: "core",
                table: "LoanApplications");

            migrationBuilder.DropColumn(
                name: "ExistingEmiAmount",
                schema: "core",
                table: "LoanApplications");

            migrationBuilder.DropColumn(
                name: "FirstName",
                schema: "core",
                table: "LoanApplications");

            migrationBuilder.DropColumn(
                name: "Gender",
                schema: "core",
                table: "LoanApplications");

            migrationBuilder.DropColumn(
                name: "LastName",
                schema: "core",
                table: "LoanApplications");

            migrationBuilder.DropColumn(
                name: "MonthlyIncome",
                schema: "core",
                table: "LoanApplications");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                schema: "core",
                table: "LoanApplications");

            migrationBuilder.DropColumn(
                name: "State",
                schema: "core",
                table: "LoanApplications");
        }
    }
}
