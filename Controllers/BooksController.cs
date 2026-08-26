using LibraryManagementSystem.Models;
using LibraryManagementSystem.Models.ViewModels;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.Controllers;

[Authorize(Roles = "Librarian")]
public class BooksController : Controller
{
    private readonly IBookRegistrationService _bookService;

    public BooksController(IBookRegistrationService bookService)
    {
        _bookService = bookService;
    }

    public IActionResult Register() => View(new BookRegistrationViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(BookRegistrationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _bookService.RegisterBookAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["Success"] = result.Message;
        TempData["BookNumber"] = result.Data;
        return RedirectToAction(nameof(Register));
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        ViewBag.Books = await _bookService.GetAllBooksAsync(page);
        return View();
    }

    public async Task<IActionResult> Edit(int id)
    {
        var model = await _bookService.GetBookForEditAsync(id);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(BookEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _bookService.UpdateBookAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCopyStatus(int bookId, int copyId, CopyAvailability status)
    {
        var result = await _bookService.UpdateCopyStatusAsync(copyId, status);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Edit), new { id = bookId });
    }
}
