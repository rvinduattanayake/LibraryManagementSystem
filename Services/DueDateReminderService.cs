using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services;

public interface IDueDateReminderService
{
    /// <summary>
    /// Sends one reminder notification per active loan that is due within
    /// LibraryConstants.DueSoonReminderDays or already overdue, skipping loans that already
    /// got a reminder. Returns how many reminders were sent.
    /// </summary>
    Task<int> SendDueSoonRemindersAsync();
}

public class DueDateReminderService : IDueDateReminderService
{
    private readonly LibraryDbContext _db;

    public DueDateReminderService(LibraryDbContext db)
    {
        _db = db;
    }

    public async Task<int> SendDueSoonRemindersAsync()
    {
        var now = DateTime.UtcNow;
        var reminderThreshold = now.AddDays(LibraryConstants.DueSoonReminderDays);

        var loansNeedingReminder = await _db.Loans
            .Include(l => l.Borrower)
            .Include(l => l.BookCopy)
            .ThenInclude(c => c.BookTitle)
            .Where(l => l.Status == LoanStatus.Active &&
                        l.DueDate != null &&
                        l.DueDate <= reminderThreshold &&
                        l.ReminderSentAt == null)
            .ToListAsync();

        if (loansNeedingReminder.Count == 0)
        {
            return 0;
        }

        foreach (var loan in loansNeedingReminder)
        {
            var isOverdue = loan.DueDate < now;
            var message = isOverdue
                ? $"'{loan.BookCopy.BookTitle.Title}' (copy {loan.BookCopy.AccessionNumber}) was due on {loan.DueDate:yyyy-MM-dd} and is now overdue. Please return it as soon as possible."
                : $"'{loan.BookCopy.BookTitle.Title}' (copy {loan.BookCopy.AccessionNumber}) is due on {loan.DueDate:yyyy-MM-dd}.";

            _db.Notifications.Add(new LibraryNotification
            {
                BorrowerId = loan.BorrowerId,
                Message = message
            });

            loan.ReminderSentAt = now;
        }

        await _db.SaveChangesAsync();
        return loansNeedingReminder.Count;
    }
}

/// <summary>Runs the due-date reminder sweep automatically on a fixed interval.</summary>
public class DueDateReminderBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DueDateReminderBackgroundService> _logger;

    public DueDateReminderBackgroundService(IServiceScopeFactory scopeFactory, ILogger<DueDateReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var reminderService = scope.ServiceProvider.GetRequiredService<IDueDateReminderService>();
                var sent = await reminderService.SendDueSoonRemindersAsync();
                if (sent > 0)
                {
                    _logger.LogInformation("Sent {Count} due-date reminder notification(s).", sent);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Due-date reminder sweep failed.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
