using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workly.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueEmployeeSalaryIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmployeeSalaries_UserId",
                table: "EmployeeSalaries");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSalaries_UserId_Month_Year",
                table: "EmployeeSalaries",
                columns: new[] { "UserId", "Month", "Year" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmployeeSalaries_UserId_Month_Year",
                table: "EmployeeSalaries");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSalaries_UserId",
                table: "EmployeeSalaries",
                column: "UserId");
        }
    }
}
