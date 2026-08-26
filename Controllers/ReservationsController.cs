using LibraryManagementSystem.Models.ViewModels;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.Controllers;

public class ReservationsController : Controller
{
    private readonly IReservationService _reservationService;

    public ReservationsController(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        ViewBag.Reservations = await _reservationService.GetReservationsAsync(page);
        return View(new ReservationRequestViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReservationRequestViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Reservations = await _reservationService.GetReservationsAsync();
            return View("Index", model);
        }

        var result = await _reservationService.CreateReservationAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            ViewBag.Reservations = await _reservationService.GetReservationsAsync();
            return View("Index", model);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Librarian")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _reservationService.CancelReservationAsync(id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
