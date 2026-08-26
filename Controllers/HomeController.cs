using LibraryManagementSystem.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers;

public class HomeController : Controller
{
    private readonly LibraryDbContext _db;

    public HomeController(LibraryDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.BookCount = await _db.BookTitles.CountAsync();
        ViewBag.CopyCount = await _db.BookCopies.CountAsync();
        ViewBag.BorrowerCount = await _db.Borrowers.CountAsync();
        ViewBag.ActiveLoans = await _db.Loans.CountAsync(l => l.Status == Models.LoanStatus.Active);
        ViewBag.PendingLoans = await _db.Loans.CountAsync(l => l.Status == Models.LoanStatus.Pending);
        ViewBag.Reservations = await _db.Reservations.CountAsync();
        return View();
    }

    public IActionResult Error() => View();
}
