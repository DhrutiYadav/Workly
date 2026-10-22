using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Workly.Data;
using Workly.Models;

namespace Workly.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var usersQuery = _context.Users.AsNoTracking();

            // Managers can see regular employees in their department.
            if (User.IsInRole("Manager"))
            {
                var currentUserId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (!int.TryParse(currentUserId, out int managerId))
                {
                    return Forbid();
                }

                var manager = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == managerId);

                if (manager == null)
                {
                    return Forbid();
                }

                usersQuery = usersQuery.Where(u =>
                    u.Department == manager.Department &&
                    u.Role == Role.User);
            }
            else
            {
                // Admins can see all employees except Admin accounts.
                usersQuery = usersQuery.Where(u =>
                    u.Role != Role.Admin);
            }

            var employees = await usersQuery.ToListAsync();

            var startDate = new DateTime(
                DateTime.Today.Year,
                DateTime.Today.Month,
                1);

            var endDate = startDate.AddMonths(1);

            var employeeIds = employees
                .Select(u => u.Id)
                .ToList();

            // Basic employee summaries.
            ViewBag.TotalEmployees = employees.Count;

            ViewBag.TotalDepartments = employees
                .Select(u => u.Department)
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Distinct()
                .Count();

            ViewBag.TotalMonthlySalary = employees
                .Where(u => u.SalaryType == SalaryType.MonthlySalary)
                .Sum(u => u.Salary);

            ViewBag.TotalDailyWageEmployees = employees
                .Count(u => u.SalaryType == SalaryType.DailyWage);

            // Attendance summary for the current month.
            ViewBag.TotalAttendanceRecords = await _context.Attendances
                .AsNoTracking()
                .CountAsync(a =>
                    employeeIds.Contains(a.EmployeeId) &&
                    a.AttendanceDate >= startDate &&
                    a.AttendanceDate < endDate);

            return View();
        }

        public async Task<IActionResult> EmployeeReport(string? search)
        {
            var usersQuery = _context.Users
                .AsNoTracking()
                .Where(u => u.Role != Role.Admin);

            // Managers can see only regular employees
            // in their own department.
            if (User.IsInRole("Manager"))
            {
                var currentUserId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (!int.TryParse(currentUserId, out int managerId))
                {
                    return Forbid();
                }

                var manager = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == managerId);

                if (manager == null)
                {
                    return Forbid();
                }

                usersQuery = usersQuery.Where(u =>
                    u.Department == manager.Department &&
                    u.Role == Role.User);
            }

            // Search employees.
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                usersQuery = usersQuery.Where(u =>
                    u.FullName.Contains(search) ||
                    u.Email.Contains(search) ||
                    u.Department.Contains(search) ||
                    u.Role.ToString().Contains(search));
            }

            // Sort alphabetically by employee name.
            var employees = await usersQuery
                .OrderBy(u => u.FullName)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.TotalEmployees = employees.Count;

            return View(employees);
        }

        [HttpGet]
        public async Task<IActionResult> EmployeeReportData(
            string? search,
            string? department,
            string? role)
        {
            var usersQuery = _context.Users
                .AsNoTracking()
                .Where(u => u.Role != Role.Admin);

            // Restrict Managers to regular employees in their department.
            if (User.IsInRole("Manager"))
            {
                var currentUserId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (!int.TryParse(currentUserId, out int managerId))
                {
                    return Forbid();
                }

                var manager = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == managerId);

                if (manager == null)
                {
                    return Forbid();
                }

                usersQuery = usersQuery.Where(u =>
                    u.Department == manager.Department &&
                    u.Role == Role.User);
            }

            // Apply employee search.
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                usersQuery = usersQuery.Where(u =>
                    u.FullName.Contains(search) ||
                    u.Email.Contains(search) ||
                    u.Department.Contains(search) ||
                    u.Role.ToString().Contains(search));
            }

            // Apply department filter.
            if (!string.IsNullOrWhiteSpace(department))
            {
                department = department.Trim();

                usersQuery = usersQuery.Where(u =>
                    u.Department == department);
            }

            // Apply role filter.
            if (!string.IsNullOrWhiteSpace(role) &&
                Enum.TryParse<Role>(role, true, out var selectedRole) &&
                selectedRole != Role.Admin &&
                Enum.IsDefined(typeof(Role), selectedRole))
            {
                usersQuery = usersQuery.Where(u =>
                    u.Role == selectedRole);
            }

            // Retrieve the filtered employees.
            var employees = await usersQuery
                .OrderBy(u => u.FullName)
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.Department,
                    Role = u.Role.ToString(),
                    u.Salary,
                    SalaryType = u.SalaryType.ToString(),
                    u.DateOfJoining
                })
                .ToListAsync();

            // Department chart uses the filtered employees.
            var departmentData = employees
                .GroupBy(e => e.Department)
                .Select(g => new
                {
                    department = g.Key,
                    count = g.Count()
                })
                .OrderByDescending(x => x.count)
                .ToList();

            // Role chart uses the filtered employees.
            var roleData = employees
                .GroupBy(e => e.Role)
                .Select(g => new
                {
                    role = g.Key,
                    count = g.Count()
                })
                .OrderByDescending(x => x.count)
                .ToList();

            return Json(new
            {
                totalEmployees = employees.Count,
                employees,
                departmentData,
                roleData
            });
        }
    }
}