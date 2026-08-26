using LibraryManagementSystem.Models.ViewModels;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.Controllers;

[Authorize(Roles = "Librarian")]
public class ReturnsController : Controller
{
    private readonly IReturnService _returnService;

    public ReturnsController(IReturnService returnService)
    {
        _returnService = returnService;
    }

    public IActionResult Index() => View(new ReturnRequestViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Process(ReturnRequestViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        var result = await _returnService.ProcessReturnAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View("Index", model);
        }

        TempData["Success"] = result.Message;
        if (result.Data?.ReservationNotice is not null)
        {
            ViewBag.ReservationNotice = result.Data.ReservationNotice;
            ViewBag.NotifiedBorrower = result.Data.NotifiedBorrower;
        }
        return View("Index", new ReturnRequestViewModel());
    }
}
