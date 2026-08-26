using LibraryManagementSystem.Models.ViewModels;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.Controllers;

public class LoansController : Controller
{
    private readonly ILoanService _loanService;

    public LoansController(ILoanService loanService)
    {
        _loanService = loanService;
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        ViewBag.PendingLoans = await _loanService.GetPendingLoansAsync(page);
        return View(new LoanRequestViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitRequest(LoanRequestViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.PendingLoans = await _loanService.GetPendingLoansAsync();
            return View("Index", model);
        }

        var result = await _loanService.CreateLoanRequestAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            ViewBag.PendingLoans = await _loanService.GetPendingLoansAsync();
            return View("Index", model);
        }


        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Librarian")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id)
    {
        var result = await _loanService.ConfirmLoanAsync(id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Librarian")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _loanService.CancelLoanAsync(id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
