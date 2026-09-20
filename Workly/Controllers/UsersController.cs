
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Workly.Models;
using Workly.Data;

public class UsersController : Controller
{
    private readonly ApplicationDbContext _context;

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

        var users = _context.Users.AsQueryable();

        // Search
        if (!string.IsNullOrWhiteSpace(search))
        {
            users = users.Where(u =>
                u.FullName.Contains(search) ||
                u.Email.Contains(search) ||
                u.Department.Contains(search) ||
                u.Role.ToString().Contains(search));
        }

        // Sorting
        switch (sortOrder)
        {
            case "name_desc":
                users = users.OrderByDescending(u => u.FullName);
                break;

            case "salary_asc":
                users = users.OrderBy(u => u.Salary);
                break;

            case "salary_desc":
                users = users.OrderByDescending(u => u.Salary);
                break;

            case "date_asc":
                users = users.OrderBy(u => u.DateOfJoining);
                break;

            case "date_desc":
                users = users.OrderByDescending(u => u.DateOfJoining);
                break;

            case "role":
                users = users.OrderBy(u => u.Role);
                break;

            case "department":
                users = users.OrderBy(u => u.Department);
                break;

            default:
                users = users.OrderBy(u => u.FullName);
                break;
        }

        // Total users
        var totalUsers = await users.CountAsync();

        // Total pages
        var totalPages = (int)Math.Ceiling(
            totalUsers / (double)pageSize);

        // Make sure page is valid
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
        var user = new User
        {
            DateOfJoining = DateTime.Today
        };

        return View(user);
    }

    // POST: USERS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,FullName,Email,Password,Role,Salary,Department,DateOfJoining")] User user)
    {
        if (ModelState.IsValid)
        {
            _context.Add(user);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(user);
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
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("Id,FullName,Email,Password,Role,Salary,Department,DateOfJoining")] User user)
    {
        if (id != user.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(user);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(user.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
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
