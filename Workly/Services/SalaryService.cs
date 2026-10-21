using Microsoft.EntityFrameworkCore;
using Workly.Data;
using Workly.Models;

namespace Workly.Services
{
    public class SalaryService
    {
        private readonly ApplicationDbContext _context;

        public SalaryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<decimal> CalculateBasicSalaryAsync(
            int userId,
            int month,
            int year)
        {
            if (month < 1 || month > 12)
            {
                throw new ArgumentOutOfRangeException(nameof(month));
            }

            if (year < 2000 || year > 2100)
            {
                throw new ArgumentOutOfRangeException(nameof(year));
            }

            var employee = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (employee == null)
            {
                throw new InvalidOperationException(
                    "Employee was not found.");
            }

            int daysInMonth = DateTime.DaysInMonth(year, month);

            decimal dailyRate = employee.SalaryType ==
                SalaryType.MonthlySalary
                ? employee.Salary / daysInMonth
                : employee.Salary;

            var attendanceRecords = await _context.Attendances
                .Where(a =>
                    a.EmployeeId == userId &&
                    a.AttendanceDate.Month == month &&
                    a.AttendanceDate.Year == year)
                .ToListAsync();

            decimal totalSalary = 0;

            foreach (var attendance in attendanceRecords)
            {
                switch (attendance.Status)
                {
                    case AttendanceStatus.Present:
                        totalSalary += dailyRate;
                        break;

                    case AttendanceStatus.HalfDay:
                        totalSalary += dailyRate * 0.5m;
                        break;

                    case AttendanceStatus.Absent:
                    case AttendanceStatus.Leave:
                        break;
                }
            }

            return Math.Round(totalSalary, 2);
        }

        public async Task<EmployeeSalary> GenerateMonthlySalaryAsync(
    int userId,
    int month,
    int year)
        {
            // Check whether a salary record already exists.
            var existingSalary = await _context.EmployeeSalaries
                .FirstOrDefaultAsync(s =>
                    s.UserId == userId &&
                    s.Month == month &&
                    s.Year == year);

            // Never automatically modify a paid salary.
            if (existingSalary != null &&
                existingSalary.PaymentStatus == "Paid")
            {
                throw new InvalidOperationException(
                    "This salary has already been paid and cannot be regenerated.");
            }

            // Calculate salary using the latest attendance records.
            decimal basicSalary = await CalculateBasicSalaryAsync(
                userId,
                month,
                year);

            // Create a new record if one doesn't exist.
            if (existingSalary == null)
            {
                existingSalary = new EmployeeSalary
                {
                    UserId = userId,
                    Month = month,
                    Year = year,
                    PaymentStatus = "Pending",
                    Allowances = 0,
                    Deductions = 0
                };

                _context.EmployeeSalaries.Add(existingSalary);
            }

            // Update the salary calculation.
            existingSalary.BasicSalary = basicSalary;
            existingSalary.NetSalary =
                basicSalary + existingSalary.Allowances -
                existingSalary.Deductions;

            // Count recorded attendance days.
            existingSalary.PresentDays = await _context.Attendances
                .CountAsync(a =>
                    a.EmployeeId == userId &&
                    a.AttendanceDate.Month == month &&
                    a.AttendanceDate.Year == year &&
                    a.Status == AttendanceStatus.Present);

            existingSalary.HalfDays = await _context.Attendances
                .CountAsync(a =>
                    a.EmployeeId == userId &&
                    a.AttendanceDate.Month == month &&
                    a.AttendanceDate.Year == year &&
                    a.Status == AttendanceStatus.HalfDay);

            await _context.SaveChangesAsync();

            return existingSalary;
        }
    }
}