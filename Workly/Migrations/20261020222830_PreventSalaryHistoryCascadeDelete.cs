using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workly.Migrations
{
    /// <inheritdoc />
    public partial class PreventSalaryHistoryCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeSalaries_Users_UserId",
                table: "EmployeeSalaries");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeSalaries_Users_UserId",
                table: "EmployeeSalaries",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeSalaries_Users_UserId",
                table: "EmployeeSalaries");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeSalaries_Users_UserId",
                table: "EmployeeSalaries",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
