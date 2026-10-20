using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Workly.Data;
using Workly.Models;

namespace Workly.Controllers
{
    [Authorize]
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AttendanceController(ApplicationDbContext context)
        {
            _context = context;
        }


        // GET: /Attendance
        // Display attendance records according to the user's role
        // and the selected month and year.
        public async Task<IActionResult> Index(int? month, int? year)
        {
            var currentUser = await GetCurrentUserAsync();

            if (currentUser == null)
            {
                return Forbid();
            }

            // Default to the current month and year.
            int selectedMonth = month ?? DateTime.Today.Month;
            int selectedYear = year ?? DateTime.Today.Year;

            // Validate the selected month.
            if (selectedMonth < 1 || selectedMonth > 12)
            {
                selectedMonth = DateTime.Today.Month;
            }

            // Validate the selected year.
            if (selectedYear < 1 || selectedYear > 9998)
            {
                selectedYear = DateTime.Today.Year;
            }

            // Calculate the beginning and end of the selected month.
            var startDate = new DateTime(
                selectedYear,
                selectedMonth,
                1);

            var endDate = startDate.AddMonths(1);

            var query = _context.Attendances
                .AsNoTracking()
                .Include(a => a.Employee)
                .AsQueryable();

            // Admin can view all employees' attendance.
            if (currentUser.Role == Role.Admin)
            {
                // No employee restriction.
            }
            // Manager can view their own attendance and regular
            // employees in their department.
            else if (currentUser.Role == Role.Manager)
            {
                query = query.Where(a =>
                    a.EmployeeId == currentUser.Id ||
                    (
                        a.Employee != null &&
                        a.Employee.Role == Role.User &&
                        a.Employee.Department == currentUser.Department
                    ));
            }
            // Regular employees can view only their own attendance.
            else
            {
                query = query.Where(a =>
                    a.EmployeeId == currentUser.Id);
            }

            // Filter attendance records by the selected month.
            query = query.Where(a =>
                a.AttendanceDate >= startDate &&
                a.AttendanceDate < endDate);

            var attendanceRecords = await query
                .OrderByDescending(a => a.AttendanceDate)
                .ToListAsync();

            // Calculate attendance summary counts for the selected month.
            ViewBag.PresentCount = attendanceRecords.Count(a =>
                a.Status == AttendanceStatus.Present);

            ViewBag.AbsentCount = attendanceRecords.Count(a =>
                a.Status == AttendanceStatus.Absent);

            ViewBag.HalfDayCount = attendanceRecords.Count(a =>
                a.Status == AttendanceStatus.HalfDay);

            ViewBag.LeaveCount = attendanceRecords.Count(a =>
                a.Status == AttendanceStatus.Leave);

            // Preserve the selected month and year for the view.
            ViewBag.SelectedMonth = selectedMonth;
            ViewBag.SelectedYear = selectedYear;

            return View(attendanceRecords);
        }

        // GET: /Attendance/Details/5
        // Display attendance details according to the user's role.
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var currentUser = await GetCurrentUserAsync();

            if (currentUser == null)
            {
                return Forbid();
            }

            var attendance = await _context.Attendances
                .AsNoTracking()
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attendance == null)
            {
                return NotFound();
            }

            // Admin can view every attendance record.
            if (currentUser.Role == Role.Admin)
            {
                return View(attendance);
            }

            // Managers can view their own attendance.
            if (currentUser.Role == Role.Manager &&
                attendance.EmployeeId == currentUser.Id)
            {
                return View(attendance);
            }

            // Managers can view regular employees in their department.
            if (currentUser.Role == Role.Manager &&
                attendance.Employee != null &&
                attendance.Employee.Role == Role.User &&
                attendance.Employee.Department == currentUser.Department)
            {
                return View(attendance);
            }

            // Regular employees can view only their own attendance.
            if (currentUser.Role == Role.User &&
                attendance.EmployeeId == currentUser.Id)
            {
                return View(attendance);
            }

            return Forbid();
        }

        // GET: /Attendance/Edit/5
        // Display the edit form only if the current user has permission.
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var currentUser = await GetCurrentUserAsync();

            if (currentUser == null)
            {
                return Forbid();
            }

            var attendance = await _context.Attendances
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attendance == null)
            {
                return NotFound();
            }

            if (!CanEditAttendance(currentUser, attendance))
            {
                return Forbid();
            }

            return View(attendance);
        }

        // POST: /Attendance/Edit/5
        // Update an attendance record after checking permissions and validation.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("AttendanceDate,Status,Notes")] Attendance model)
        {
            var currentUser = await GetCurrentUserAsync();

            if (currentUser == null)
            {
                return Forbid();
            }

            var attendance = await _context.Attendances
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attendance == null)
            {
                return NotFound();
            }

            if (!CanEditAttendance(currentUser, attendance))
            {
                return Forbid();
            }

            // Reject future attendance dates.
            if (model.AttendanceDate.Date > DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(model.AttendanceDate),
                    "Attendance date cannot be in the future.");
            }

            // Reject invalid status values.
            if (!Enum.IsDefined(typeof(AttendanceStatus), model.Status))
            {
                ModelState.AddModelError(
                    nameof(model.Status),
                    "Please select a valid attendance status.");
            }

            // Prevent duplicate attendance for the same employee and date.
            bool duplicateExists = await _context.Attendances.AnyAsync(a =>
                a.EmployeeId == attendance.EmployeeId &&
                a.AttendanceDate == model.AttendanceDate.Date &&
                a.Id != attendance.Id);

            if (duplicateExists)
            {
                ModelState.AddModelError(
                    nameof(model.AttendanceDate),
                    "Attendance already exists for this employee on that date.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Update only the permitted fields.
            // EmployeeId remains unchanged.
            attendance.AttendanceDate = model.AttendanceDate.Date;
            attendance.Status = model.Status;
            attendance.Notes = model.Notes?.Trim();

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // The unique database index protects against concurrent duplicates.
                bool duplicateNowExists = await _context.Attendances.AnyAsync(a =>
                    a.EmployeeId == attendance.EmployeeId &&
                    a.AttendanceDate == model.AttendanceDate.Date &&
                    a.Id != attendance.Id);

                if (!duplicateNowExists)
                {
                    throw;
                }

                ModelState.AddModelError(
                    nameof(model.AttendanceDate),
                    "Attendance already exists for this employee on that date.");

                return View(model);
            }

            TempData["SuccessMessage"] =
                "Attendance updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // Check whether the current user is allowed to edit this record.
        private static bool CanEditAttendance(
            User currentUser,
            Attendance attendance)
        {
            // Admin can edit every attendance record.
            if (currentUser.Role == Role.Admin)
            {
                return true;
            }

            // Managers can edit their own attendance.
            if (currentUser.Role == Role.Manager &&
                attendance.EmployeeId == currentUser.Id)
            {
                return true;
            }

            // Managers can also edit regular employees in their department.
            if (currentUser.Role == Role.Manager &&
                attendance.Employee != null &&
                attendance.Employee.Role == Role.User &&
                attendance.Employee.Department == currentUser.Department)
            {
                return true;
            }

            // Regular employees cannot edit attendance.
            return false;
        }

        // GET: /Attendance/Create
        // Display the form for recording the logged-in user's attendance.
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var currentUser = await GetCurrentUserAsync();

            if (currentUser == null)
            {
                return Forbid();
            }

            var model = new Attendance
            {
                AttendanceDate = DateTime.Today,
                EmployeeId = currentUser.Id
            };

            return View(model);
        }

        // POST: /Attendance/Create
        // The employee identity always comes from the authenticated login.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("AttendanceDate,Status,Notes")] Attendance model)
        {
            var currentUser = await GetCurrentUserAsync();

            if (currentUser == null)
            {
                return Forbid();
            }

            // Always assign the authenticated user's ID.
            // Never accept an employee ID from the submitted form.
            model.EmployeeId = currentUser.Id;

            // Reject future dates on the server.
            if (model.AttendanceDate.Date > DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(model.AttendanceDate),
                    "Attendance date cannot be in the future.");
            }

            // Reject invalid attendance status values.
            if (!Enum.IsDefined(
                    typeof(AttendanceStatus),
                    model.Status))
            {
                ModelState.AddModelError(
                    nameof(model.Status),
                    "Please select a valid attendance status.");
            }

            // Prevent duplicate attendance for this user and date.
            bool duplicateExists = await _context.Attendances
                .AnyAsync(a =>
                    a.EmployeeId == currentUser.Id &&
                    a.AttendanceDate == model.AttendanceDate.Date);

            if (duplicateExists)
            {
                ModelState.AddModelError(
                    nameof(model.AttendanceDate),
                    "You have already recorded attendance for this date.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Create a fresh entity containing only permitted values.
            var attendance = new Attendance
            {
                EmployeeId = currentUser.Id,
                AttendanceDate = model.AttendanceDate.Date,
                Status = model.Status,
                Notes = model.Notes?.Trim()
            };

            _context.Attendances.Add(attendance);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // The unique database index also protects against
                // simultaneous duplicate submissions.
                bool duplicateNowExists =
                    await _context.Attendances.AnyAsync(a =>
                        a.EmployeeId == currentUser.Id &&
                        a.AttendanceDate == model.AttendanceDate.Date);

                if (!duplicateNowExists)
                {
                    throw;
                }

                ModelState.AddModelError(
                    nameof(model.AttendanceDate),
                    "You have already recorded attendance for this date.");

                return View(model);
            }

            TempData["SuccessMessage"] =
                "Your attendance has been recorded successfully.";

            return RedirectToAction(nameof(Index));
        }

        // Find the authenticated user using the login identity.
        private async Task<User?> GetCurrentUserAsync()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userId, out int id))
            {
                return null;
            }

            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);
        }
    }
}