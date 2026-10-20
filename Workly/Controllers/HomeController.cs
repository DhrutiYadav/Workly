using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;
using Workly.Data;
using Workly.Models;

namespace Workly.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel();

            // Get the logged-in user's role.
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            // Get the logged-in user's department.
            var department = User.FindFirst("Department")?.Value;

            // Admin can see company-wide statistics.
            // Manager can see statistics for their department.
            // Regular users won't receive company-wide statistics.
            if (role == nameof(Role.Admin) ||
                (role == nameof(Role.Manager) &&
                 !string.IsNullOrWhiteSpace(department)))
            {
                var employeesQuery = _context.Users.AsNoTracking();

                if (role == nameof(Role.Manager))
                {
                    employeesQuery = employeesQuery.Where(
                        u => u.Department == department);
                }

                var today = DateTime.Today;
                var recentJoinersStart = today.AddDays(-30);

                model.TotalEmployees = await employeesQuery.CountAsync();

                model.TotalManagers = await employeesQuery
                    .CountAsync(u => u.Role == Role.Manager);

                model.TotalDepartments = await employeesQuery
                    .Select(u => u.Department)
                    .Distinct()
                    .CountAsync();

                model.RecentJoiners = await employeesQuery
                    .CountAsync(u =>
                        u.DateOfJoining >= recentJoinersStart &&
                        u.DateOfJoining <= today);

                // Department Overview
                model.Departments = await employeesQuery
                    .GroupBy(u => u.Department)
                    .Select(group => new DepartmentSummaryViewModel
                    {
                        DepartmentName = group.Key,
                        EmployeeCount = group.Count()
                    })
                    .OrderByDescending(d => d.EmployeeCount)
                    .ThenBy(d => d.DepartmentName)
                    .ToListAsync();
            }

            // Recently Joined Employees
            var recentEmployeesQuery = _context.Users
                .AsNoTracking()
                .AsQueryable();

            // Admin can view all employees.
            if (role == nameof(Role.Admin))
            {
                // No additional filter is required.
            }
            // Manager can view employees in their own department.
            else if (role == nameof(Role.Manager))
            {
                if (string.IsNullOrWhiteSpace(department))
                {
                    return Forbid();
                }

                recentEmployeesQuery = recentEmployeesQuery
                    .Where(u => u.Department == department);
            }
            // Regular users can view their own information.
            else
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(userId, out var id))
                {
                    return Forbid();
                }

                recentEmployeesQuery = recentEmployeesQuery
                    .Where(u => u.Id == id);
            }

            // Get the five most recently joined employees.
            model.RecentEmployees = await recentEmployeesQuery
                .OrderByDescending(u => u.DateOfJoining)
                .ThenByDescending(u => u.Id)
                .Take(5)
                .Select(u => new RecentEmployeeViewModel
                {
                    FullName = u.FullName,
                    Department = u.Department,
                    Role = u.Role.ToString(),
                    DateOfJoining = u.DateOfJoining
                })
                .ToListAsync();

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id
                    ?? HttpContext.TraceIdentifier
            });
        }
    }
}