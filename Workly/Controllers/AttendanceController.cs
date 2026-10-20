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
        // Display records according to the logged-in user's role.
        public async Task<IActionResult> Index()
        {
            var currentUser = await GetCurrentUserAsync();

            if (currentUser == null)
            {
                return Forbid();
            }

            var query = _context.Attendances
                .AsNoTracking()
                .Include(a => a.Employee)
                .AsQueryable();

            if (currentUser.Role == Role.Admin)
            {
                // Admin can view all attendance records.
            }
            else if (currentUser.Role == Role.Manager)
            {
                // Manager can view their own attendance and the
                // attendance of regular employees in their department.
                query = query.Where(a =>
                    a.EmployeeId == currentUser.Id ||
                    (
                        a.Employee != null &&
                        a.Employee.Role == Role.User &&
                        a.Employee.Department == currentUser.Department
                    ));
            }
            else
            {
                // Regular employees can view only their own records.
                query = query.Where(a =>
                    a.EmployeeId == currentUser.Id);
            }

            var attendanceRecords = await query
                .OrderByDescending(a => a.AttendanceDate)
                .ToListAsync();

            return View(attendanceRecords);
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