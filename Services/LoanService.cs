using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services;

public interface ILoanService
{
    Task<OperationResult<List<Loan>>> CreateLoanRequestAsync(LoanRequestViewModel model);
    Task<OperationResult<List<Loan>>> CreateLoanRequestForBorrowerAsync(int borrowerId, string accessionNumbers);
    Task<OperationResult<Loan>> ConfirmLoanAsync(int loanId);
    Task<OperationResult<Loan>> CancelLoanAsync(int loanId);
    Task<OperationResult<Loan>> CancelLoanForBorrowerAsync(int loanId, int borrowerId);
    Task<PagedResult<LoanActionViewModel>> GetPendingLoansAsync(int page = 1, int pageSize = LibraryConstants.DefaultPageSize);
    Task<PagedResult<LoanActionViewModel>> GetLoansForBorrowerAsync(int borrowerId, int page = 1, int pageSize = LibraryConstants.DefaultPageSize);
}

public class LoanService : ILoanService
{
    private readonly LibraryDbContext _db;
    private readonly IReservationService _reservationService;

    public LoanService(LibraryDbContext db, IReservationService reservationService)
    {
        _db = db;
        _reservationService = reservationService;
    }

    public async Task<OperationResult<List<Loan>>> CreateLoanRequestAsync(LoanRequestViewModel model)
    {
        var borrower = await _db.Borrowers.FirstOrDefaultAsync(b => b.UserNumber == model.UserNumber.Trim());
        if (borrower is null)
        {
            return OperationResult<List<Loan>>.Fail($"Borrower with user number '{model.UserNumber}' was not found.");
        }

        return await CreateLoanRequestCoreAsync(borrower, model.AccessionNumbers);
    }

    public async Task<OperationResult<List<Loan>>> CreateLoanRequestForBorrowerAsync(int borrowerId, string accessionNumbers)
    {
        var borrower = await _db.Borrowers.FindAsync(borrowerId);
        if (borrower is null)
        {
            return OperationResult<List<Loan>>.Fail("Borrower record not found.");
        }

        return await CreateLoanRequestCoreAsync(borrower, accessionNumbers);
    }

    private async Task<OperationResult<List<Loan>>> CreateLoanRequestCoreAsync(Borrower borrower, string accessionNumbersRaw)
    {
        await _reservationService.ReleaseExpiredHoldsAsync();

        var validation = await ValidateBorrowerEligibilityAsync(borrower.Id);
        if (!validation.Success)
        {
            return OperationResult<List<Loan>>.Fail(validation.Message);
        }

        var accessionNumbers = accessionNumbersRaw
            .Split(['\r', '\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (accessionNumbers.Count == 0)
        {
            return OperationResult<List<Loan>>.Fail("Please provide at least one accession number.");
        }

        var activeCount = await GetActiveLoanCountAsync(borrower.Id);
        if (activeCount + accessionNumbers.Count > LibraryConstants.MaxBooksPerBorrower)
        {
            return OperationResult<List<Loan>>.Fail(
                $"Borrower may have at most {LibraryConstants.MaxBooksPerBorrower} books on loan. Currently on loan: {activeCount}.");
        }

        var createdLoans = new List<Loan>();

        foreach (var accession in accessionNumbers)
        {
            var copy = await _db.BookCopies
                .Include(c => c.BookTitle)
                .FirstOrDefaultAsync(c => c.AccessionNumber == accession);

            if (copy is null)
            {
                return OperationResult<List<Loan>>.Fail($"Copy with accession number '{accession}' was not found.");
            }

            if (copy.CopyType == CopyType.Reference)
            {
                return OperationResult<List<Loan>>.Fail(
                    $"Copy '{accession}' ({copy.BookTitle.Title}) is reference-only and cannot be borrowed.");
            }

            if (copy.Availability != CopyAvailability.Available)
            {
                return OperationResult<List<Loan>>.Fail(
                    $"Copy '{accession}' is not available (status: {copy.Availability}).");
            }

            if (await _db.Loans.AnyAsync(l => l.BookCopyId == copy.Id && l.Status == LoanStatus.Pending))
            {
                return OperationResult<List<Loan>>.Fail($"Copy '{accession}' already has a pending loan request.");
            }

            var loan = new Loan
            {
                BorrowerId = borrower.Id,
                BookCopyId = copy.Id,
                Status = LoanStatus.Pending
            };

            _db.Loans.Add(loan);
            createdLoans.Add(loan);
        }

        await _db.SaveChangesAsync();
        return OperationResult<List<Loan>>.Ok(createdLoans, $"Created {createdLoans.Count} pending loan request(s) for librarian approval.");
    }

    public async Task<OperationResult<Loan>> ConfirmLoanAsync(int loanId)
    {
        await _reservationService.ReleaseExpiredHoldsAsync();

        var loan = await _db.Loans
            .Include(l => l.Borrower)
            .Include(l => l.BookCopy)
            .ThenInclude(c => c.BookTitle)
            .FirstOrDefaultAsync(l => l.Id == loanId);

        if (loan is null)
        {
            return OperationResult<Loan>.Fail("Loan request not found.");
        }

        if (loan.Status != LoanStatus.Pending)
        {
            return OperationResult<Loan>.Fail($"Loan is not pending (current status: {loan.Status}).");
        }

        var validation = await ValidateBorrowerEligibilityAsync(loan.BorrowerId);
        if (!validation.Success)
        {
            return OperationResult<Loan>.Fail(validation.Message);
        }

        if (loan.BookCopy.CopyType == CopyType.Reference)
        {
            return OperationResult<Loan>.Fail("Reference copies cannot be borrowed.");
        }

        if (loan.BookCopy.Availability != CopyAvailability.Available)
        {
            return OperationResult<Loan>.Fail("Copy is no longer available.");
        }

        var activeCount = await GetActiveLoanCountAsync(loan.BorrowerId);
        if (activeCount >= LibraryConstants.MaxBooksPerBorrower)
        {
            return OperationResult<Loan>.Fail(
                $"Borrower already has the maximum of {LibraryConstants.MaxBooksPerBorrower} books on loan.");
        }

        var loanDate = DateTime.UtcNow;
        loan.Status = LoanStatus.Active;
        loan.LoanDate = loanDate;
        loan.DueDate = loanDate.AddDays(LibraryConstants.LoanPeriodDays);
        loan.BookCopy.Availability = CopyAvailability.OnLoan;
        loan.BookCopy.Version++;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<Loan>.Fail(
                "This copy was just allocated by another action (e.g. a return or another confirmation). Please refresh and try again.");
        }

        return OperationResult<Loan>.Ok(
            loan,
            $"Loan confirmed. Expected return date: {loan.DueDate:yyyy-MM-dd} ({LibraryConstants.LoanPeriodDays} days).");
    }

    public async Task<OperationResult<Loan>> CancelLoanAsync(int loanId)
    {
        var loan = await _db.Loans.FirstOrDefaultAsync(l => l.Id == loanId);
        if (loan is null)
        {
            return OperationResult<Loan>.Fail("Loan request not found.");
        }

        return await CancelLoanCoreAsync(loan);
    }

    public async Task<OperationResult<Loan>> CancelLoanForBorrowerAsync(int loanId, int borrowerId)
    {
        var loan = await _db.Loans.FirstOrDefaultAsync(l => l.Id == loanId);
        if (loan is null || loan.BorrowerId != borrowerId)
        {
            return OperationResult<Loan>.Fail("Loan request not found.");
        }

        return await CancelLoanCoreAsync(loan);
    }

    private async Task<OperationResult<Loan>> CancelLoanCoreAsync(Loan loan)
    {
        if (loan.Status != LoanStatus.Pending)
        {
            return OperationResult<Loan>.Fail("Only pending loan requests can be cancelled.");
        }

        loan.Status = LoanStatus.Cancelled;
        await _db.SaveChangesAsync();

        return OperationResult<Loan>.Ok(loan, "Loan request cancelled.");
    }

    public async Task<PagedResult<LoanActionViewModel>> GetPendingLoansAsync(int page = 1, int pageSize = LibraryConstants.DefaultPageSize)
    {
        page = Math.Max(1, page);
        var query = _db.Loans
            .Where(l => l.Status == LoanStatus.Pending)
            .Include(l => l.Borrower)
            .Include(l => l.BookCopy)
            .ThenInclude(c => c.BookTitle)
            .OrderBy(l => l.RequestedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LoanActionViewModel
            {
                LoanId = l.Id,
                BorrowerName = l.Borrower.Name,
                AccessionNumber = l.BookCopy.AccessionNumber,
                BookTitle = l.BookCopy.BookTitle.Title,
                RequestedAt = l.RequestedAt,
                Status = l.Status,
                DueDate = l.DueDate,
                FineAmount = l.FineAmount
            })
            .ToListAsync();

        return new PagedResult<LoanActionViewModel>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PagedResult<LoanActionViewModel>> GetLoansForBorrowerAsync(int borrowerId, int page = 1, int pageSize = LibraryConstants.DefaultPageSize)
    {
        page = Math.Max(1, page);
        var query = _db.Loans
            .Where(l => l.BorrowerId == borrowerId)
            .Include(l => l.Borrower)
            .Include(l => l.BookCopy)
            .ThenInclude(c => c.BookTitle)
            .OrderByDescending(l => l.RequestedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LoanActionViewModel
            {
                LoanId = l.Id,
                BorrowerName = l.Borrower.Name,
                AccessionNumber = l.BookCopy.AccessionNumber,
                BookTitle = l.BookCopy.BookTitle.Title,
                RequestedAt = l.RequestedAt,
                Status = l.Status,
                DueDate = l.DueDate,
                FineAmount = l.FineAmount
            })
            .ToListAsync();

        return new PagedResult<LoanActionViewModel>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    private async Task<OperationResult> ValidateBorrowerEligibilityAsync(int borrowerId)
    {
        var hasOverdue = await _db.Loans.AnyAsync(l =>
            l.BorrowerId == borrowerId &&
            l.Status == LoanStatus.Active &&
            l.DueDate < DateTime.UtcNow);

        if (hasOverdue)
        {
            return OperationResult.Fail(
                "Borrower has overdue books. They cannot borrow until overdue books are returned.");
        }

        return OperationResult.Ok(string.Empty);
    }

    private Task<int> GetActiveLoanCountAsync(int borrowerId) =>
        _db.Loans.CountAsync(l => l.BorrowerId == borrowerId && l.Status == LoanStatus.Active);
}
