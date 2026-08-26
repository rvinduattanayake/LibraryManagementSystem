using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services;

public interface IReservationService
{
    Task<OperationResult<Reservation>> CreateReservationAsync(ReservationRequestViewModel model);
    Task<OperationResult<Reservation>> CreateReservationForBorrowerAsync(int borrowerId, string bookSearch);
    Task<PagedResult<ReservationListItemViewModel>> GetReservationsAsync(int page = 1, int pageSize = LibraryConstants.DefaultPageSize);
    Task<PagedResult<ReservationListItemViewModel>> GetReservationsForBorrowerAsync(int borrowerId, int page = 1, int pageSize = LibraryConstants.DefaultPageSize);
    Task<OperationResult> CancelReservationAsync(int reservationId);
    Task<OperationResult> CancelReservationForBorrowerAsync(int reservationId, int borrowerId);

    /// <summary>
    /// Releases any AwaitingPickup holds whose pickup window has expired: hands the copy to the
    /// next FIFO reservation if one exists, otherwise returns it to Available. Safe to call
    /// opportunistically before any availability-sensitive decision.
    /// </summary>
    Task ReleaseExpiredHoldsAsync();
}

public class ReservationService : IReservationService
{
    private readonly LibraryDbContext _db;

    public ReservationService(LibraryDbContext db)
    {
        _db = db;
    }

    public async Task<OperationResult<Reservation>> CreateReservationAsync(ReservationRequestViewModel model)
    {
        var borrower = await _db.Borrowers.FirstOrDefaultAsync(b => b.UserNumber == model.UserNumber.Trim());
        if (borrower is null)
        {
            return OperationResult<Reservation>.Fail($"Borrower '{model.UserNumber}' was not found.");
        }

        return await CreateReservationCoreAsync(borrower, model.BookSearch);
    }

    public async Task<OperationResult<Reservation>> CreateReservationForBorrowerAsync(int borrowerId, string bookSearch)
    {
        var borrower = await _db.Borrowers.FindAsync(borrowerId);
        if (borrower is null)
        {
            return OperationResult<Reservation>.Fail("Borrower record not found.");
        }

        return await CreateReservationCoreAsync(borrower, bookSearch);
    }

    private async Task<OperationResult<Reservation>> CreateReservationCoreAsync(Borrower borrower, string bookSearchRaw)
    {
        await ReleaseExpiredHoldsAsync();

        var search = bookSearchRaw.Trim();
        var title = await _db.BookTitles
            .Include(t => t.Copies)
            .FirstOrDefaultAsync(t =>
                t.BookNumber == search ||
                t.Title.Contains(search) ||
                t.Author.Contains(search));

        if (title is null)
        {
            return OperationResult<Reservation>.Fail("Book title was not found in the catalogue.");
        }

        var hasAvailableBorrowable = title.Copies.Any(c =>
            c.CopyType == CopyType.Borrowable &&
            c.Availability == CopyAvailability.Available);

        if (hasAvailableBorrowable)
        {
            return OperationResult<Reservation>.Fail(
                "A borrowable copy is currently available. Please borrow the book directly instead of reserving.");
        }

        if (await _db.Reservations.AnyAsync(r => r.BorrowerId == borrower.Id && r.BookTitleId == title.Id))
        {
            return OperationResult<Reservation>.Fail("Borrower already has a reservation for this title.");
        }

        var reservation = new Reservation
        {
            BorrowerId = borrower.Id,
            BookTitleId = title.Id
        };

        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync();

        return OperationResult<Reservation>.Ok(
            reservation,
            $"Reservation placed for '{title.Title}'. You will be notified when a copy is returned.");
    }

    public async Task<PagedResult<ReservationListItemViewModel>> GetReservationsAsync(int page = 1, int pageSize = LibraryConstants.DefaultPageSize)
    {
        await ReleaseExpiredHoldsAsync();

        page = Math.Max(1, page);
        var query = _db.Reservations
            .Include(r => r.Borrower)
            .Include(r => r.BookTitle)
            .OrderBy(r => r.BookTitleId)
            .ThenBy(r => r.ReservedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReservationListItemViewModel
            {
                Id = r.Id,
                BorrowerName = r.Borrower.Name,
                UserNumber = r.Borrower.UserNumber,
                BookTitle = r.BookTitle.Title,
                BookNumber = r.BookTitle.BookNumber,
                ReservedAt = r.ReservedAt
            })
            .ToListAsync();

        return new PagedResult<ReservationListItemViewModel>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PagedResult<ReservationListItemViewModel>> GetReservationsForBorrowerAsync(int borrowerId, int page = 1, int pageSize = LibraryConstants.DefaultPageSize)
    {
        await ReleaseExpiredHoldsAsync();

        page = Math.Max(1, page);
        var query = _db.Reservations
            .Where(r => r.BorrowerId == borrowerId)
            .Include(r => r.Borrower)
            .Include(r => r.BookTitle)
            .OrderBy(r => r.ReservedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReservationListItemViewModel
            {
                Id = r.Id,
                BorrowerName = r.Borrower.Name,
                UserNumber = r.Borrower.UserNumber,
                BookTitle = r.BookTitle.Title,
                BookNumber = r.BookTitle.BookNumber,
                ReservedAt = r.ReservedAt
            })
            .ToListAsync();

        return new PagedResult<ReservationListItemViewModel>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<OperationResult> CancelReservationAsync(int reservationId)
    {
        var reservation = await _db.Reservations.FindAsync(reservationId);
        if (reservation is null)
        {
            return OperationResult.Fail("Reservation not found.");
        }

        _db.Reservations.Remove(reservation);
        await _db.SaveChangesAsync();
        return OperationResult.Ok("Reservation cancelled.");
    }

    public async Task<OperationResult> CancelReservationForBorrowerAsync(int reservationId, int borrowerId)
    {
        var reservation = await _db.Reservations.FindAsync(reservationId);
        if (reservation is null || reservation.BorrowerId != borrowerId)
        {
            return OperationResult.Fail("Reservation not found.");
        }

        _db.Reservations.Remove(reservation);
        await _db.SaveChangesAsync();
        return OperationResult.Ok("Reservation cancelled.");
    }

    public async Task ReleaseExpiredHoldsAsync()
    {
        var now = DateTime.UtcNow;
        var expiredCopies = await _db.BookCopies
            .Include(c => c.BookTitle)
            .Where(c => c.Availability == CopyAvailability.AwaitingPickup && c.AwaitingPickupExpiresAt < now)
            .ToListAsync();

        if (expiredCopies.Count == 0)
        {
            return;
        }

        foreach (var copy in expiredCopies)
        {
            var nextReservation = await _db.Reservations
                .Include(r => r.Borrower)
                .Where(r => r.BookTitleId == copy.BookTitleId)
                .OrderBy(r => r.ReservedAt)
                .FirstOrDefaultAsync();

            if (nextReservation is not null)
            {
                _db.Notifications.Add(new LibraryNotification
                {
                    BorrowerId = nextReservation.BorrowerId,
                    Message =
                        $"Your reserved book '{copy.BookTitle.Title}' (copy {copy.AccessionNumber}) is now available. " +
                        $"Please collect it from the library counter within {LibraryConstants.PickupWindowDays} days."
                });
                _db.Reservations.Remove(nextReservation);
                copy.AwaitingBorrowerId = nextReservation.BorrowerId;
                copy.AwaitingPickupExpiresAt = now.AddDays(LibraryConstants.PickupWindowDays);
            }
            else
            {
                copy.Availability = CopyAvailability.Available;
                copy.AwaitingBorrowerId = null;
                copy.AwaitingPickupExpiresAt = null;
            }

            copy.Version++;
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another action already touched one of these copies (e.g. a return or loan confirm
            // in flight). Skip this best-effort sweep; it will retry on the next call.
        }
    }
}
