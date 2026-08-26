using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services;

public interface IReportService
{
    Task<ReportsViewModel> GetReportsAsync();
}

public class ReportService : IReportService
{
    private const int PopularTitleCount = 5;
    private const int ReservationBacklogCount = 5;

    private readonly LibraryDbContext _db;

    public ReportService(LibraryDbContext db)
    {
        _db = db;
    }

    public async Task<ReportsViewModel> GetReportsAsync()
    {
        var now = DateTime.UtcNow;

        var overdueLoans = await _db.Loans
            .Include(l => l.Borrower)
            .Include(l => l.BookCopy)
            .ThenInclude(c => c.BookTitle)
            .Where(l => l.Status == LoanStatus.Active && l.DueDate != null && l.DueDate < now)
            .OrderBy(l => l.DueDate)
            .Select(l => new OverdueLoanViewModel
            {
                LoanId = l.Id,
                BorrowerName = l.Borrower.Name,
                UserNumber = l.Borrower.UserNumber,
                BookTitle = l.BookCopy.BookTitle.Title,
                AccessionNumber = l.BookCopy.AccessionNumber,
                DueDate = l.DueDate!.Value
            })
            .ToListAsync();

        foreach (var loan in overdueLoans)
        {
            loan.DaysOverdue = (int)Math.Ceiling((now - loan.DueDate).TotalDays);
        }

        var activeLoanCount = await _db.Loans.CountAsync(l => l.Status == LoanStatus.Active);

        var outstandingFines = await _db.Loans
            .Where(l => l.FineAmount > 0 && l.FinePaidAt == null)
            .ToListAsync();

        var popularTitles = await _db.Loans
            .Where(l => l.Status == LoanStatus.Active || l.Status == LoanStatus.Returned)
            .Select(l => new { l.BookCopy.BookTitle.Title, l.BookCopy.BookTitle.BookNumber })
            .GroupBy(x => new { x.Title, x.BookNumber })
            .Select(g => new PopularTitleViewModel
            {
                Title = g.Key.Title,
                BookNumber = g.Key.BookNumber,
                LoanCount = g.Count()
            })
            .OrderByDescending(p => p.LoanCount)
            .Take(PopularTitleCount)
            .ToListAsync();

        var reservationBacklog = await _db.Reservations
            .Select(r => new { r.BookTitle.Title, r.BookTitle.BookNumber })
            .GroupBy(x => new { x.Title, x.BookNumber })
            .Select(g => new ReservationBacklogItemViewModel
            {
                Title = g.Key.Title,
                BookNumber = g.Key.BookNumber,
                ReservationCount = g.Count()
            })
            .OrderByDescending(r => r.ReservationCount)
            .Take(ReservationBacklogCount)
            .ToListAsync();

        var totalActiveReservations = await _db.Reservations.CountAsync();

        return new ReportsViewModel
        {
            OverdueLoans = overdueLoans,
            ActiveLoanCount = activeLoanCount,
            TotalOutstandingFines = outstandingFines.Sum(l => l.FineAmount),
            OutstandingFineCount = outstandingFines.Count,
            PopularTitles = popularTitles,
            ReservationBacklog = reservationBacklog,
            TotalActiveReservations = totalActiveReservations
        };
    }
}
