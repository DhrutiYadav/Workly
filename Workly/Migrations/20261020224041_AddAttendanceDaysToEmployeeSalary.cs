using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workly.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceDaysToEmployeeSalary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HalfDays",
                table: "EmployeeSalaries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PresentDays",
                table: "EmployeeSalaries",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HalfDays",
                table: "EmployeeSalaries");

            migrationBuilder.DropColumn(
                name: "PresentDays",
                table: "EmployeeSalaries");
        }
    }
}
