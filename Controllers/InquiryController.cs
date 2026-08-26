using LibraryManagementSystem.Models.ViewModels;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.Controllers;

public class InquiryController : Controller
{
    private readonly IInquiryService _inquiryService;

    public InquiryController(IInquiryService inquiryService)
    {
        _inquiryService = inquiryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] InquirySearchViewModel model, int page = 1)
    {
        var hasQuery = !string.IsNullOrWhiteSpace(model.AccessionNumber) ||
                       !string.IsNullOrWhiteSpace(model.Title) ||
                       !string.IsNullOrWhiteSpace(model.Author) ||
                       !string.IsNullOrWhiteSpace(model.Genre) ||
                       model.AvailableOnly;

        if (hasQuery)
        {
            ViewBag.Results = await _inquiryService.SearchAsync(model, page);
        }

        ViewBag.Facets = await _inquiryService.GetCatalogueFacetsAsync();
        return View(model);
    }
}
