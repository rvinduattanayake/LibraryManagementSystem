using LibraryManagementSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly LibraryDbContext _db;

    public HomeController(LibraryDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        if (User.IsInRole("Member"))
        {
            return RedirectToAction("MyLoans", "Member");
        }
        if (!User.IsInRole("Librarian"))
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        ViewBag.BookCount = await _db.BookTitles.CountAsync();
        ViewBag.CopyCount = await _db.BookCopies.CountAsync();
        ViewBag.BorrowerCount = await _db.Borrowers.CountAsync();
        ViewBag.ActiveLoans = await _db.Loans.CountAsync(l => l.Status == Models.LoanStatus.Active);
        ViewBag.PendingLoans = await _db.Loans.CountAsync(l => l.Status == Models.LoanStatus.Pending);
        ViewBag.Reservations = await _db.Reservations.CountAsync();
        ViewBag.OverdueLoans = await _db.Loans.CountAsync(l =>
            l.Status == Models.LoanStatus.Active && l.DueDate != null && l.DueDate < DateTime.UtcNow);
        return View();
    }

    [AllowAnonymous]
    public IActionResult Error() => View();
}
