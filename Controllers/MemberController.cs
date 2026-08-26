using LibraryManagementSystem.Models;
using LibraryManagementSystem.Models.ViewModels;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.Controllers;

[Authorize(Roles = "Member")]
public class MemberController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserRegistrationService _userRegistrationService;
    private readonly ILoanService _loanService;
    private readonly IReservationService _reservationService;
    private readonly IFineService _fineService;
    private readonly INotificationService _notificationService;

    public MemberController(
        UserManager<ApplicationUser> userManager,
        IUserRegistrationService userRegistrationService,
        ILoanService loanService,
        IReservationService reservationService,
        IFineService fineService,
        INotificationService notificationService)
    {
        _userManager = userManager;
        _userRegistrationService = userRegistrationService;
        _loanService = loanService;
        _reservationService = reservationService;
        _fineService = fineService;
        _notificationService = notificationService;
    }

    private async Task<Borrower?> GetCurrentBorrowerAsync()
    {
        var userId = _userManager.GetUserId(User);
        return userId is null ? null : await _userRegistrationService.GetBorrowerByApplicationUserIdAsync(userId);
    }

    public async Task<IActionResult> MyLoans(int page = 1)
    {
        var borrower = await GetCurrentBorrowerAsync();
        if (borrower is null)
        {
            return NotFound();
        }

        ViewBag.Loans = await _loanService.GetLoansForBorrowerAsync(borrower.Id, page);
        return View(new MemberLoanRequestViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitLoanRequest(MemberLoanRequestViewModel model)
    {
        var borrower = await GetCurrentBorrowerAsync();
        if (borrower is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Loans = await _loanService.GetLoansForBorrowerAsync(borrower.Id);
            return View(nameof(MyLoans), model);
        }

        var result = await _loanService.CreateLoanRequestForBorrowerAsync(borrower.Id, model.AccessionNumbers);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            ViewBag.Loans = await _loanService.GetLoansForBorrowerAsync(borrower.Id);
            return View(nameof(MyLoans), model);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(MyLoans));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelMyLoan(int id)
    {
        var borrower = await GetCurrentBorrowerAsync();
        if (borrower is null)
        {
            return NotFound();
        }

        var result = await _loanService.CancelLoanForBorrowerAsync(id, borrower.Id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(MyLoans));
    }

    public async Task<IActionResult> MyReservations(int page = 1)
    {
        var borrower = await GetCurrentBorrowerAsync();
        if (borrower is null)
        {
            return NotFound();
        }

        ViewBag.Reservations = await _reservationService.GetReservationsForBorrowerAsync(borrower.Id, page);
        return View(new MemberReservationRequestViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateMyReservation(MemberReservationRequestViewModel model)
    {
        var borrower = await GetCurrentBorrowerAsync();
        if (borrower is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Reservations = await _reservationService.GetReservationsForBorrowerAsync(borrower.Id);
            return View(nameof(MyReservations), model);
        }

        var result = await _reservationService.CreateReservationForBorrowerAsync(borrower.Id, model.BookSearch);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            ViewBag.Reservations = await _reservationService.GetReservationsForBorrowerAsync(borrower.Id);
            return View(nameof(MyReservations), model);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(MyReservations));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelMyReservation(int id)
    {
        var borrower = await GetCurrentBorrowerAsync();
        if (borrower is null)
        {
            return NotFound();
        }

        var result = await _reservationService.CancelReservationForBorrowerAsync(id, borrower.Id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(MyReservations));
    }

    public async Task<IActionResult> MyFines(int page = 1)
    {
        var borrower = await GetCurrentBorrowerAsync();
        if (borrower is null)
        {
            return NotFound();
        }

        ViewBag.Fines = await _fineService.GetFinesForBorrowerAsync(borrower.Id, page);
        return View();
    }

    public async Task<IActionResult> MyNotifications(int page = 1)
    {
        var borrower = await GetCurrentBorrowerAsync();
        if (borrower is null)
        {
            return NotFound();
        }

        ViewBag.Notifications = await _notificationService.GetNotificationsForBorrowerAsync(borrower.Id, page);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkMyNotificationRead(int id)
    {
        var borrower = await GetCurrentBorrowerAsync();
        if (borrower is null)
        {
            return NotFound();
        }

        var result = await _notificationService.MarkAsReadForBorrowerAsync(id, borrower.Id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(MyNotifications));
    }
}
