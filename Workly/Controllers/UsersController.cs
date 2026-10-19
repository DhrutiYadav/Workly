
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Workly.Models;
using Workly.Data;
using Microsoft.AspNetCore.Identity;
public class UsersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public UsersController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: USERS
    // GET: USERS
    public async Task<IActionResult> Index(
        string search,
        string sortOrder,
        int page = 1)
    {
        const int pageSize = 10;

        var users = _context.Users.AsQueryable();

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

    // GET: USERS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }
        return View(user);
    }

    // POST: USERS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    // POST: USERS/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int? id,
        [Bind("Id,FullName,Email,Password,Role,Salary,Department,DateOfJoining")] User user)
    {
        if (id != user.Id)
        {
            return NotFound();
        }

        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == user.Id);

        if (existingUser == null)
        {
            return NotFound();
        }

        // Preserve the existing password hash
        user.PasswordHash = existingUser.PasswordHash;

        // If a new password was entered, hash it
        if (!string.IsNullOrWhiteSpace(user.Password))
        {
            user.PasswordHash = _passwordHasher.HashPassword(
                user,
                user.Password);
        }
        else
        {
            // No new password: keep the current hash
            ModelState.Remove(nameof(user.Password));
        }

        // Duplicate email validation
        bool emailExists = await _context.Users
            .AnyAsync(u =>
                u.Email == user.Email &&
                u.Id != user.Id);

        if (emailExists)
        {
            ModelState.AddModelError(
                "Email",
                "A user with this email already exists.");
        }

        if (ModelState.IsValid)
        {
            // Update only the editable fields
            existingUser.FullName = user.FullName;
            existingUser.Email = user.Email;
            existingUser.Role = user.Role;
            existingUser.Salary = user.Salary;
            existingUser.Department = user.Department;
            existingUser.DateOfJoining = user.DateOfJoining;
            existingUser.PasswordHash = user.PasswordHash;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        return View(user);
    }

    // GET: USERS/Delete/5
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
}
