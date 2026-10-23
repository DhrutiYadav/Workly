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

        public async Task<IActionResult> AttendanceReport(
    int? employeeId,
    DateTime? startDate,
    DateTime? endDate,
    string? status)
        {
            // Start with attendance records and employee details.
            var query = _context.Attendances
                .AsNoTracking()
                .Include(a => a.Employee)
                .AsQueryable();

            // Restrict Managers to their own attendance and
            // regular employees in their department.
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

                query = query.Where(a =>
                    a.EmployeeId == manager.Id ||
                    (
                        a.Employee != null &&
                        a.Employee.Role == Role.User &&
                        a.Employee.Department == manager.Department
                    ));
            }
            else
            {
                // Admins can view all attendance records.
                query = query.Where(a =>
                    a.Employee != null &&
                    a.Employee.Role != Role.Admin);
            }

            // Apply employee filter.
            if (employeeId.HasValue)
            {
                query = query.Where(a =>
                    a.EmployeeId == employeeId.Value);
            }

            // Apply start-date filter.
            if (startDate.HasValue)
            {
                query = query.Where(a =>
                    a.AttendanceDate >= startDate.Value.Date);
            }

            // Apply end-date filter.
            if (endDate.HasValue)
            {
                query = query.Where(a =>
                    a.AttendanceDate < endDate.Value.Date.AddDays(1));
            }

            // Apply attendance-status filter.
            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<AttendanceStatus>(
                    status, true, out var selectedStatus) &&
                Enum.IsDefined(typeof(AttendanceStatus), selectedStatus))
            {
                query = query.Where(a =>
                    a.Status == selectedStatus);
            }

            // Retrieve the filtered attendance records.
            var attendanceRecords = await query
                .OrderByDescending(a => a.AttendanceDate)
                .ThenBy(a => a.Employee!.FullName)
                .ToListAsync();

            // Populate the employee dropdown with permitted employees.
            var employeesQuery = _context.Users
                .AsNoTracking()
                .Where(u => u.Role != Role.Admin);

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

                employeesQuery = employeesQuery.Where(u =>
                    u.Id == manager.Id ||
                    (u.Role == Role.User &&
                     u.Department == manager.Department));
            }

            ViewBag.Employees = await employeesQuery
                .OrderBy(u => u.FullName)
                .ToListAsync();

            // Preserve selected filters.
            ViewBag.SelectedEmployeeId = employeeId;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.SelectedStatus = status;

            // Summary cards reflect the filtered records.
            ViewBag.TotalRecords = attendanceRecords.Count;
            ViewBag.PresentCount = attendanceRecords.Count(a =>
                a.Status == AttendanceStatus.Present);
            ViewBag.AbsentCount = attendanceRecords.Count(a =>
                a.Status == AttendanceStatus.Absent);
            ViewBag.HalfDayCount = attendanceRecords.Count(a =>
                a.Status == AttendanceStatus.HalfDay);
            ViewBag.LeaveCount = attendanceRecords.Count(a =>
                a.Status == AttendanceStatus.Leave);

            return View(attendanceRecords);
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

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SalaryReport(
    int? employeeId,
    int? month,
    int? year,
    string? paymentStatus)
        {
            var query = _context.EmployeeSalaries
                .AsNoTracking()
                .Include(s => s.User)
                .Where(s => s.User.Role != Role.Admin)
                .AsQueryable();

            // Filter by employee.
            if (employeeId.HasValue)
            {
                query = query.Where(s => s.UserId == employeeId.Value);
            }

            // Filter by month.
            if (month.HasValue && month.Value >= 1 && month.Value <= 12)
            {
                query = query.Where(s => s.Month == month.Value);
            }

            // Filter by year.
            if (year.HasValue && year.Value >= 2000 && year.Value <= 2100)
            {
                query = query.Where(s => s.Year == year.Value);
            }

            // Filter by payment status.
            if (!string.IsNullOrWhiteSpace(paymentStatus) &&
                (paymentStatus == "Paid" || paymentStatus == "Pending"))
            {
                query = query.Where(s => s.PaymentStatus == paymentStatus);
            }

            var salaries = await query
                .OrderByDescending(s => s.Year)
                .ThenByDescending(s => s.Month)
                .ThenBy(s => s.User.FullName)
                .ToListAsync();

            // Populate employee dropdown.
            ViewBag.Employees = await _context.Users
                .AsNoTracking()
                .Where(u => u.Role != Role.Admin)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            // Preserve selected filters.
            ViewBag.SelectedEmployeeId = employeeId;
            ViewBag.SelectedMonth = month;
            ViewBag.SelectedYear = year;
            ViewBag.SelectedPaymentStatus = paymentStatus;

            // Summary cards.
            ViewBag.TotalRecords = salaries.Count;
            ViewBag.TotalBasicSalary = salaries.Sum(s => s.BasicSalary);
            ViewBag.TotalAllowances = salaries.Sum(s => s.Allowances);
            ViewBag.TotalDeductions = salaries.Sum(s => s.Deductions);
            ViewBag.TotalNetSalary = salaries.Sum(s => s.NetSalary);

            ViewBag.PaidCount = salaries.Count(s => s.PaymentStatus == "Paid");
            ViewBag.PendingCount = salaries.Count(s => s.PaymentStatus == "Pending");

            return View(salaries);
        }
    }
}