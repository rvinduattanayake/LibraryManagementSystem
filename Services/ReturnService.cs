using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services;

public interface IReturnService
{
    Task<OperationResult<ReturnResultViewModel>> ProcessReturnAsync(ReturnRequestViewModel model);
}

public class ReturnService : IReturnService
{
    private readonly LibraryDbContext _db;
    private readonly IReservationService _reservationService;
    private readonly IFineService _fineService;

    public ReturnService(LibraryDbContext db, IReservationService reservationService, IFineService fineService)
    {
        _db = db;
        _reservationService = reservationService;
        _fineService = fineService;
    }

    public async Task<OperationResult<ReturnResultViewModel>> ProcessReturnAsync(ReturnRequestViewModel model)
    {
        await _reservationService.ReleaseExpiredHoldsAsync();

        var copy = await _db.BookCopies
            .Include(c => c.BookTitle)
            .FirstOrDefaultAsync(c => c.AccessionNumber == model.AccessionNumber.Trim());

        if (copy is null)
        {
            return OperationResult<ReturnResultViewModel>.Fail(
                $"Copy with accession number '{model.AccessionNumber}' was not found.");
        }

        var activeLoan = await _db.Loans
            .Include(l => l.Borrower)
            .FirstOrDefaultAsync(l => l.BookCopyId == copy.Id && l.Status == LoanStatus.Active);

        if (activeLoan is null)
        {
            return OperationResult<ReturnResultViewModel>.Fail(
                $"No active loan found for copy '{copy.AccessionNumber}'.");
        }

        activeLoan.Status = LoanStatus.Returned;
        activeLoan.ReturnedAt = DateTime.UtcNow;

        var result = new ReturnResultViewModel
        {
            Success = true,
            Message = $"Return accepted for '{copy.AccessionNumber}' ({copy.BookTitle.Title})."
        };

        if (activeLoan.DueDate is { } dueDate && activeLoan.ReturnedAt.Value > dueDate)
        {
            activeLoan.FineAmount = _fineService.CalculateFine(dueDate, activeLoan.ReturnedAt.Value);
            result.Message += $" Overdue fine: {activeLoan.FineAmount:C}.";
        }

        var oldestReservation = await _db.Reservations
            .Include(r => r.Borrower)
            .Where(r => r.BookTitleId == copy.BookTitleId)
            .OrderBy(r => r.ReservedAt)
            .FirstOrDefaultAsync();

        if (oldestReservation is not null)
        {
            var notification = new LibraryNotification
            {
                BorrowerId = oldestReservation.BorrowerId,
                Message =
                    $"Your reserved book '{copy.BookTitle.Title}' (copy {copy.AccessionNumber}) is now available. " +
                    "Please collect it from the library counter within 3 days."
            };

            _db.Notifications.Add(notification);
            _db.Reservations.Remove(oldestReservation);

            copy.Availability = CopyAvailability.AwaitingPickup;
            copy.AwaitingBorrowerId = oldestReservation.BorrowerId;
            copy.AwaitingPickupExpiresAt = DateTime.UtcNow.AddDays(LibraryConstants.PickupWindowDays);

            result.ReservationNotice =
                $"Outstanding reservation found for title '{copy.BookTitle.Title}'. " +
                $"Copy has been set aside for the member with the oldest reservation (pickup window: {LibraryConstants.PickupWindowDays} days).";
            result.NotifiedBorrower =
                $"{oldestReservation.Borrower.Name} ({oldestReservation.Borrower.UserNumber})";
        }
        else
        {
            copy.Availability = CopyAvailability.Available;
            copy.AwaitingBorrowerId = null;
            copy.AwaitingPickupExpiresAt = null;
        }

        copy.Version++;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<ReturnResultViewModel>.Fail(
                $"Copy '{copy.AccessionNumber}' was just updated by another action. Please try the return again.");
        }

        return OperationResult<ReturnResultViewModel>.Ok(result, result.Message);
    }
}
