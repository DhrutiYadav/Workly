using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Workly.Data;
using Workly.Models;
using Workly.Services;

namespace Workly.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SalaryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SalaryService _salaryService;

        public SalaryController(
            ApplicationDbContext context,
            SalaryService salaryService)
        {
            _context = context;
            _salaryService = salaryService;
        }

        public async Task<IActionResult> Index(
            int? month,
            int? year)
        {
            int selectedMonth = month ?? DateTime.Today.Month;
            int selectedYear = year ?? DateTime.Today.Year;

            if (selectedMonth < 1 || selectedMonth > 12)
            {
                selectedMonth = DateTime.Today.Month;
            }

            if (selectedYear < 2000 || selectedYear > 2100)
            {
                selectedYear = DateTime.Today.Year;
            }

            var salaries = await _context.EmployeeSalaries
                .AsNoTracking()
                .Include(s => s.User)
                .Where(s =>
                    s.Month == selectedMonth &&
                    s.Year == selectedYear)
                .OrderBy(s => s.User.FullName)
                .ToListAsync();

            ViewBag.SelectedMonth = selectedMonth;
            ViewBag.SelectedYear = selectedYear;

            return View(salaries);
        }

        // GET: Salary/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var salary = await _context.EmployeeSalaries
                .AsNoTracking()
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (salary == null)
            {
                return NotFound();
            }

            return View(salary);
        }

        // GET: Salary/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var salary = await _context.EmployeeSalaries
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (salary == null)
            {
                return NotFound();
            }

            if (salary.PaymentStatus == "Paid")
            {
                TempData["ErrorMessage"] =
                    "Paid salary records cannot be edited.";

                return RedirectToAction(nameof(Details), new { id = salary.Id });
            }

            return View(salary);
        }

        // POST: Salary/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            decimal basicSalary,
            decimal allowances,
            decimal deductions)
        {
            var salary = await _context.EmployeeSalaries
                .FirstOrDefaultAsync(s => s.Id == id);

            if (salary == null)
            {
                return NotFound();
            }

            if (salary.PaymentStatus == "Paid")
            {
                TempData["ErrorMessage"] =
                    "Paid salary records cannot be edited.";

                return RedirectToAction(nameof(Details), new { id = salary.Id });
            }

            if (basicSalary < 0 || basicSalary > 10000000 ||
                allowances < 0 || allowances > 10000000 ||
                deductions < 0 || deductions > 10000000)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Salary amounts must be between ₹0 and ₹1,00,00,000.");

                salary.User = await _context.Users
                    .FirstAsync(u => u.Id == salary.UserId);

                return View(salary);
            }

            if (basicSalary + allowances < deductions)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Deductions cannot exceed Basic Salary plus Allowances.");

                salary.User = await _context.Users
                    .FirstAsync(u => u.Id == salary.UserId);

                return View(salary);
            }

            salary.BasicSalary = basicSalary;
            salary.Allowances = allowances;
            salary.Deductions = deductions;

            salary.NetSalary =
                salary.BasicSalary + salary.Allowances - salary.Deductions;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Basic salary, allowances, and deductions updated successfully.";

            return RedirectToAction(nameof(Details), new { id = salary.Id });
        }
        // POST: Salary/MarkAsPaid/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsPaid(int id)
        {
            var salary = await _context.EmployeeSalaries
                .FirstOrDefaultAsync(s => s.Id == id);

            if (salary == null)
            {
                return NotFound();
            }

            if (salary.PaymentStatus == "Paid")
            {
                TempData["ErrorMessage"] = "This salary has already been paid.";
                return RedirectToAction(nameof(Details), new { id = salary.Id });
            }

            salary.PaymentStatus = "Paid";
            salary.PaymentDate = DateTime.Today;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Salary marked as paid successfully.";

            return RedirectToAction(nameof(Details), new { id = salary.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Generate()
        {
            ViewBag.Employees = await _context.Users
                .AsNoTracking()
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(
            int userId,
            int month,
            int year)
        {
            try
            {
                await _salaryService.GenerateMonthlySalaryAsync(
                    userId,
                    month,
                    year);

                TempData["SuccessMessage"] =
                    "Salary generated successfully.";

                return RedirectToAction(nameof(Index), new { month, year });
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;

                return RedirectToAction(nameof(Index), new { month, year });
            }
            catch (ArgumentOutOfRangeException)
            {
                TempData["ErrorMessage"] =
                    "Please select a valid month and year.";

                return RedirectToAction(nameof(Generate));
            }
        }
    }
}