using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services;

public interface IBookRegistrationService
{
    Task<OperationResult<string>> RegisterBookAsync(BookRegistrationViewModel model);
    Task<PagedResult<BookListItemViewModel>> GetAllBooksAsync(int page = 1, int pageSize = LibraryConstants.DefaultPageSize);
    Task<BookEditViewModel?> GetBookForEditAsync(int id);
    Task<OperationResult> UpdateBookAsync(BookEditViewModel model);
    Task<OperationResult> UpdateCopyStatusAsync(int copyId, CopyAvailability newStatus);
}

public class BookRegistrationService : IBookRegistrationService
{
    private readonly LibraryDbContext _db;

    public BookRegistrationService(LibraryDbContext db)
    {
        _db = db;
    }

    public async Task<OperationResult<string>> RegisterBookAsync(BookRegistrationViewModel model)
    {
        if (model.NumberOfCopies < 1 || model.NumberOfCopies > LibraryConstants.MaxCopiesPerTitle)
        {
            return OperationResult<string>.Fail(
                $"A maximum of {LibraryConstants.MaxCopiesPerTitle} copies are allowed per book number.");
        }

        var sequence = await _db.BookTitles.CountAsync(t => t.Classification == model.Classification) + 1;
        var bookNumber = $"{model.Classification.Trim()}-{sequence:D4}";

        var title = new BookTitle
        {
            BookNumber = bookNumber,
            Title = model.Title.Trim(),
            Author = model.Author.Trim(),
            Isbn = string.IsNullOrWhiteSpace(model.Isbn) ? null : model.Isbn.Trim(),
            Classification = model.Classification.Trim(),
            Publisher = model.Publisher.Trim(),
            Genre = string.IsNullOrWhiteSpace(model.Genre) ? null : model.Genre.Trim(),
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            CoverImageUrl = string.IsNullOrWhiteSpace(model.CoverImageUrl) ? null : model.CoverImageUrl.Trim()
        };

        for (var i = 1; i <= model.NumberOfCopies; i++)
        {
            title.Copies.Add(new BookCopy
            {
                CopyNumber = i,
                AccessionNumber = $"{bookNumber}-{i:D2}",
                CopyType = model.CopyType,
                Availability = CopyAvailability.Available
            });
        }

        _db.BookTitles.Add(title);
        await _db.SaveChangesAsync();

        var accessionList = string.Join(", ", title.Copies.Select(c => c.AccessionNumber));
        return OperationResult<string>.Ok(
            bookNumber,
            $"Registered title '{title.Title}' with book number {bookNumber}. Accession numbers: {accessionList}");
    }

    public async Task<PagedResult<BookListItemViewModel>> GetAllBooksAsync(int page = 1, int pageSize = LibraryConstants.DefaultPageSize)
    {
        page = Math.Max(1, page);
        var query = _db.BookTitles
            .Include(t => t.Copies)
            .OrderBy(t => t.Title);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new BookListItemViewModel
            {
                Id = t.Id,
                BookNumber = t.BookNumber,
                Title = t.Title,
                Author = t.Author,
                TotalCopies = t.Copies.Count
            })
            .ToListAsync();

        return new PagedResult<BookListItemViewModel>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<BookEditViewModel?> GetBookForEditAsync(int id)
    {
        var title = await _db.BookTitles
            .Include(t => t.Copies)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (title is null)
        {
            return null;
        }

        return new BookEditViewModel
        {
            Id = title.Id,
            BookNumber = title.BookNumber,
            Title = title.Title,
            Author = title.Author,
            Isbn = title.Isbn,
            Publisher = title.Publisher,
            Genre = title.Genre,
            Description = title.Description,
            CoverImageUrl = title.CoverImageUrl,
            Copies = title.Copies
                .OrderBy(c => c.AccessionNumber)
                .Select(c => new CopyStatusDetail
                {
                    Id = c.Id,
                    AccessionNumber = c.AccessionNumber,
                    CopyType = c.CopyType,
                    Availability = c.Availability,
                    StatusDescription = c.Availability.ToString()
                })
                .ToList()
        };
    }

    public async Task<OperationResult> UpdateBookAsync(BookEditViewModel model)
    {
        var title = await _db.BookTitles.FindAsync(model.Id);
        if (title is null)
        {
            return OperationResult.Fail("Book title not found.");
        }

        title.Title = model.Title.Trim();
        title.Author = model.Author.Trim();
        title.Isbn = string.IsNullOrWhiteSpace(model.Isbn) ? null : model.Isbn.Trim();
        title.Publisher = model.Publisher.Trim();
        title.Genre = string.IsNullOrWhiteSpace(model.Genre) ? null : model.Genre.Trim();
        title.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        title.CoverImageUrl = string.IsNullOrWhiteSpace(model.CoverImageUrl) ? null : model.CoverImageUrl.Trim();

        await _db.SaveChangesAsync();
        return OperationResult.Ok($"'{title.Title}' updated.");
    }

    public async Task<OperationResult> UpdateCopyStatusAsync(int copyId, CopyAvailability newStatus)
    {
        if (newStatus != CopyAvailability.Lost && newStatus != CopyAvailability.Withdrawn && newStatus != CopyAvailability.Available)
        {
            return OperationResult.Fail("Copy status can only be manually set to Available, Lost, or Withdrawn.");
        }

        var copy = await _db.BookCopies.FindAsync(copyId);
        if (copy is null)
        {
            return OperationResult.Fail("Copy not found.");
        }

        if (copy.Availability == CopyAvailability.OnLoan || copy.Availability == CopyAvailability.AwaitingPickup)
        {
            return OperationResult.Fail(
                $"Copy '{copy.AccessionNumber}' is currently {copy.Availability} and must be returned before its status can be changed.");
        }

        copy.Availability = newStatus;
        copy.Version++;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult.Fail($"Copy '{copy.AccessionNumber}' was just updated by another action. Please try again.");
        }

        return OperationResult.Ok($"Copy '{copy.AccessionNumber}' marked as {newStatus}.");
    }
}
