using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services;

public interface IFineService
{
    /// <summary>Pure calculation: LibraryConstants.FinePerDayOverdue per day late, rounded up to a full day.</summary>
    decimal CalculateFine(DateTime dueDate, DateTime returnDate);

    Task<PagedResult<FineListItemViewModel>> GetOutstandingFinesAsync(int page = 1, int pageSize = LibraryConstants.DefaultPageSize);
    Task<PagedResult<FineListItemViewModel>> GetFinesForBorrowerAsync(int borrowerId, int page = 1, int pageSize = LibraryConstants.DefaultPageSize);
    Task<OperationResult> MarkFinePaidAsync(int loanId);
}

public class FineService : IFineService
{
    private readonly LibraryDbContext _db;

    public FineService(LibraryDbContext db)
    {
        _db = db;
    }

    public decimal CalculateFine(DateTime dueDate, DateTime returnDate)
    {
        if (returnDate <= dueDate)
        {
            return 0m;
        }

        var daysLate = (int)Math.Ceiling((returnDate - dueDate).TotalDays);
        return daysLate * LibraryConstants.FinePerDayOverdue;
    }

    public async Task<PagedResult<FineListItemViewModel>> GetOutstandingFinesAsync(int page = 1, int pageSize = LibraryConstants.DefaultPageSize)
    {
        page = Math.Max(1, page);
        var query = _db.Loans
            .Include(l => l.Borrower)
            .Include(l => l.BookCopy)
            .ThenInclude(c => c.BookTitle)
            .Where(l => l.FineAmount > 0 && l.FinePaidAt == null)
            .OrderByDescending(l => l.ReturnedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new FineListItemViewModel
            {
                LoanId = l.Id,
                BorrowerName = l.Borrower.Name,
                UserNumber = l.Borrower.UserNumber,
                BookTitle = l.BookCopy.BookTitle.Title,
                AccessionNumber = l.BookCopy.AccessionNumber,
                DueDate = l.DueDate!.Value,
                ReturnedAt = l.ReturnedAt!.Value,
                FineAmount = l.FineAmount,
                IsPaid = l.FinePaidAt != null
            })
            .ToListAsync();

        return new PagedResult<FineListItemViewModel>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PagedResult<FineListItemViewModel>> GetFinesForBorrowerAsync(int borrowerId, int page = 1, int pageSize = LibraryConstants.DefaultPageSize)
    {
        page = Math.Max(1, page);
        var query = _db.Loans
            .Include(l => l.Borrower)
            .Include(l => l.BookCopy)
            .ThenInclude(c => c.BookTitle)
            .Where(l => l.BorrowerId == borrowerId && l.FineAmount > 0)
            .OrderByDescending(l => l.ReturnedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new FineListItemViewModel
            {
                LoanId = l.Id,
                BorrowerName = l.Borrower.Name,
                UserNumber = l.Borrower.UserNumber,
                BookTitle = l.BookCopy.BookTitle.Title,
                AccessionNumber = l.BookCopy.AccessionNumber,
                DueDate = l.DueDate!.Value,
                ReturnedAt = l.ReturnedAt!.Value,
                FineAmount = l.FineAmount,
                IsPaid = l.FinePaidAt != null
            })
            .ToListAsync();

        return new PagedResult<FineListItemViewModel>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<OperationResult> MarkFinePaidAsync(int loanId)
    {
        var loan = await _db.Loans.FindAsync(loanId);
        if (loan is null)
        {
            return OperationResult.Fail("Loan not found.");
        }

        if (loan.FineAmount <= 0 || loan.FinePaidAt is not null)
        {
            return OperationResult.Fail("This loan has no outstanding fine.");
        }

        loan.FinePaidAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return OperationResult.Ok("Fine marked as paid.");
    }
}
