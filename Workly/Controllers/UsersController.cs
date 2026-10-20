
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Workly.Data;
using Workly.Models;
using System.Security.Claims;
[Authorize(Roles = "Admin,Manager")]
public class UsersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public UsersController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: USERS
    public async Task<IActionResult> Index(
        string search,
        string sortOrder,
        int page = 1)
    {
        const int pageSize = 10;

        IQueryable<User> users = _context.Users;

        if (User.IsInRole("Manager"))
        {
            var currentUserId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(currentUserId, out int managerId))
            {
                return Forbid();
            }

            var manager = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == managerId);

            if (manager == null)
            {
                return Forbid();
            }

            // Managers can see only regular employees
            // in their own department.
            users = users.Where(u =>
                u.Department == manager.Department &&
                u.Role == Role.User);
        }

        // Search
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            users = users.Where(u =>
                u.FullName.Contains(search) ||
                u.Email.Contains(search) ||
                u.Department.Contains(search) ||
                u.Role.ToString().Contains(search));
        }

        // Sort
        switch (sortOrder)
        {
            case "name_desc":
                users = users
                    .OrderByDescending(u => u.FullName)
                    .ThenBy(u => u.Id);
                break;

            case "salary_asc":
                users = users
                    .OrderBy(u => u.Salary)
                    .ThenBy(u => u.FullName);
                break;

            case "salary_desc":
                users = users
                    .OrderByDescending(u => u.Salary)
                    .ThenBy(u => u.FullName);
                break;

            case "date_asc":
                users = users
                    .OrderBy(u => u.DateOfJoining)
                    .ThenBy(u => u.FullName);
                break;

            case "date_desc":
                users = users
                    .OrderByDescending(u => u.DateOfJoining)
                    .ThenBy(u => u.FullName);
                break;

            case "role":
                users = users
                    .OrderBy(u => u.Role)
                    .ThenBy(u => u.FullName);
                break;

            case "department":
                users = users
                    .OrderBy(u => u.Department)
                    .ThenBy(u => u.FullName);
                break;

            default:
                users = users
                    .OrderBy(u => u.FullName)
                    .ThenBy(u => u.Id);
                break;
        }

        // Total users after search
        var totalUsers = await users.CountAsync();

        // Total pages
        var totalPages = (int)Math.Ceiling(
            totalUsers / (double)pageSize);

        // Keep page valid
        if (page < 1)
        {
            page = 1;
        }

        if (totalPages > 0 && page > totalPages)
        {
            page = totalPages;
        }

        // Pagination
        var usersOnPage = await users
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Send values to View
        ViewBag.Search = search;
        ViewBag.SortOrder = sortOrder;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalUsers = totalUsers;

        return View(usersOnPage);
    }
    // GET: USERS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(m => m.Id == id);
        if (user == null)
        {
            return NotFound();
        }

        return View(user);
    }

    // GET: USERS/Create
    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        var model = new UserFormViewModel
        {
            DateOfJoining = DateTime.Today
        };

        return View(model);
    }

    // POST: USERS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    // POST: USERS/Create
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        bool emailExists = await _context.Users
            .AnyAsync(u => u.Email == model.Email);

        if (emailExists)
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "A user with this email already exists.");

            return View(model);
        }

        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(
                nameof(model.Password),
                "Password is required.");

            return View(model);
        }

        var user = new User
        {
            FullName = model.FullName,
            Email = model.Email,
            Role = model.Role,
            Salary = model.Salary,
            Department = model.Department,
            DateOfJoining = model.DateOfJoining
        };

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            model.Password);

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // GET: Users/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound();
        }

        if (!await CanManageEmployeeAsync(user))
        {
            return NotFound();
        }

        var model = new UserFormViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            Salary = user.Salary,
            Department = user.Department,
            DateOfJoining = user.DateOfJoining
        };

        return View(model);
    }

    // POST: Users/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int? id,
        UserFormViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == model.Id);

        if (existingUser == null)
        {
            return NotFound();
        }

        // Check authorization using the existing database record.
        if (!await CanManageEmployeeAsync(existingUser))
        {
            return NotFound();
        }

        bool isAdmin = User.IsInRole("Admin");

        // Managers cannot change protected fields.
        if (!isAdmin)
        {
            ModelState.Remove(nameof(model.Role));
            ModelState.Remove(nameof(model.Salary));
            ModelState.Remove(nameof(model.Department));
            ModelState.Remove(nameof(model.Password));
        }

        // Prevent duplicate email addresses.
        bool emailExists = await _context.Users
            .AnyAsync(u =>
                u.Email == model.Email &&
                u.Id != model.Id);

        if (emailExists)
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "A user with this email already exists.");
        }

        if (ModelState.IsValid)
        {
            // Fields both Admin and Manager can edit.
            existingUser.FullName = model.FullName;
            existingUser.Email = model.Email;
            existingUser.DateOfJoining = model.DateOfJoining;

            if (isAdmin)
            {
                // Only Admin can change these fields.
                existingUser.Role = model.Role;
                existingUser.Salary = model.Salary;
                existingUser.Department = model.Department;

                // Only Admin can change an employee's password here.
                if (!string.IsNullOrWhiteSpace(model.Password))
                {
                    var passwordHasher = new PasswordHasher<User>();

                    existingUser.PasswordHash =
                        passwordHasher.HashPassword(
                            existingUser,
                            model.Password);
                }
            }

            // Managers cannot change salary, role, department,
            // or password, even if they modify the submitted form.

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Restore protected values before redisplaying the form.
        if (!isAdmin)
        {
            model.Role = existingUser.Role;
            model.Salary = existingUser.Salary;
            model.Department = existingUser.Department;
            model.Password = null;
        }

        return View(model);
    }

    // GET: USERS/Delete/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(m => m.Id == id);
        if (user == null)
        {
            return NotFound();
        }

        return View(user);
    }

    // POST: USERS/Delete/5
    [Authorize(Roles = "Admin")]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user != null)
        {
            _context.Users.Remove(user);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool UserExists(int? id)
    {
        return _context.Users.Any(e => e.Id == id);
    }

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

    private async Task<bool> CanManageEmployeeAsync(User employee)
    {
        // Admin can manage every employee.
        if (User.IsInRole("Admin"))
        {
            return true;
        }

        // Only Managers can use department-based access.
        if (!User.IsInRole("Manager"))
        {
            return false;
        }

        var manager = await GetCurrentUserAsync();

        if (manager == null)
        {
            return false;
        }

        // Managers can manage regular employees
        // in their own department only.
        return employee.Role == Role.User &&
               string.Equals(
                   manager.Department,
                   employee.Department,
                   StringComparison.OrdinalIgnoreCase);
    }
}
