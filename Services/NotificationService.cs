using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services;

public interface INotificationService
{
    Task<PagedResult<NotificationListItemViewModel>> GetNotificationsAsync(int page = 1, int pageSize = LibraryConstants.DefaultPageSize);
    Task<PagedResult<NotificationListItemViewModel>> GetNotificationsForBorrowerAsync(int borrowerId, int page = 1, int pageSize = LibraryConstants.DefaultPageSize);
    Task<OperationResult> MarkAsReadAsync(int notificationId);
    Task<OperationResult> MarkAsReadForBorrowerAsync(int notificationId, int borrowerId);
}

public class NotificationService : INotificationService
{
    private readonly LibraryDbContext _db;

    public NotificationService(LibraryDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<NotificationListItemViewModel>> GetNotificationsAsync(int page = 1, int pageSize = LibraryConstants.DefaultPageSize)
    {
        page = Math.Max(1, page);
        var query = _db.Notifications
            .Include(n => n.Borrower)
            .OrderBy(n => n.IsRead)
            .ThenByDescending(n => n.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationListItemViewModel
            {
                Id = n.Id,
                BorrowerName = n.Borrower.Name,
                UserNumber = n.Borrower.UserNumber,
                Message = n.Message,
                CreatedAt = n.CreatedAt,
                IsRead = n.IsRead
            })
            .ToListAsync();

        return new PagedResult<NotificationListItemViewModel>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PagedResult<NotificationListItemViewModel>> GetNotificationsForBorrowerAsync(int borrowerId, int page = 1, int pageSize = LibraryConstants.DefaultPageSize)
    {
        page = Math.Max(1, page);
        var query = _db.Notifications
            .Where(n => n.BorrowerId == borrowerId)
            .Include(n => n.Borrower)
            .OrderBy(n => n.IsRead)
            .ThenByDescending(n => n.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationListItemViewModel
            {
                Id = n.Id,
                BorrowerName = n.Borrower.Name,
                UserNumber = n.Borrower.UserNumber,
                Message = n.Message,
                CreatedAt = n.CreatedAt,
                IsRead = n.IsRead
            })
            .ToListAsync();

        return new PagedResult<NotificationListItemViewModel>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<OperationResult> MarkAsReadAsync(int notificationId)
    {
        var notification = await _db.Notifications.FindAsync(notificationId);
        if (notification is null)
        {
            return OperationResult.Fail("Notification not found.");
        }

        notification.IsRead = true;
        await _db.SaveChangesAsync();
        return OperationResult.Ok("Notification marked as read.");
    }

    public async Task<OperationResult> MarkAsReadForBorrowerAsync(int notificationId, int borrowerId)
    {
        var notification = await _db.Notifications.FindAsync(notificationId);
        if (notification is null || notification.BorrowerId != borrowerId)
        {
            return OperationResult.Fail("Notification not found.");
        }

        notification.IsRead = true;
        await _db.SaveChangesAsync();
        return OperationResult.Ok("Notification marked as read.");
    }
}
