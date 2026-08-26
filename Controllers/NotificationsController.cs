using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.Controllers;

[Authorize(Roles = "Librarian")]
public class NotificationsController : Controller
{
    private readonly INotificationService _notificationService;
    private readonly IReservationService _reservationService;
    private readonly IDueDateReminderService _reminderService;

    public NotificationsController(
        INotificationService notificationService,
        IReservationService reservationService,
        IDueDateReminderService reminderService)
    {
        _notificationService = notificationService;
        _reservationService = reservationService;
        _reminderService = reminderService;
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        ViewBag.Notifications = await _notificationService.GetNotificationsAsync(page);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var result = await _notificationService.MarkAsReadAsync(id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReleaseExpiredHolds()
    {
        await _reservationService.ReleaseExpiredHoldsAsync();
        TempData["Success"] = "Expired pickup holds have been released.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendDueSoonReminders()
    {
        var sent = await _reminderService.SendDueSoonRemindersAsync();
        TempData["Success"] = sent == 0
            ? "No new due-date reminders were needed."
            : $"Sent {sent} due-date reminder notification(s).";
        return RedirectToAction(nameof(Index));
    }
}
