using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services;

public interface IInquiryService
{
    Task<PagedResult<InquiryResultViewModel>> SearchAsync(InquirySearchViewModel model, int page = 1, int pageSize = LibraryConstants.DefaultPageSize);
    Task<CatalogueFacets> GetCatalogueFacetsAsync();
}

public class InquiryService : IInquiryService
{
    private readonly LibraryDbContext _db;

    public InquiryService(LibraryDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<InquiryResultViewModel>> SearchAsync(InquirySearchViewModel model, int page = 1, int pageSize = LibraryConstants.DefaultPageSize)
    {
        page = Math.Max(1, page);

        var hasQuery = !string.IsNullOrWhiteSpace(model.AccessionNumber) ||
                       !string.IsNullOrWhiteSpace(model.Title) ||
                       !string.IsNullOrWhiteSpace(model.Author) ||
                       !string.IsNullOrWhiteSpace(model.Genre) ||
                       model.AvailableOnly;

        if (!hasQuery)
        {
            return new PagedResult<InquiryResultViewModel> { Page = page, PageSize = pageSize };
        }

        IQueryable<BookTitle> query = _db.BookTitles.Include(t => t.Copies);

        if (!string.IsNullOrWhiteSpace(model.AccessionNumber))
        {
            var accession = model.AccessionNumber.Trim();
            query = query.Where(t => t.Copies.Any(c => c.AccessionNumber == accession));
        }

        if (!string.IsNullOrWhiteSpace(model.Title))
        {
            var title = model.Title.Trim();
            query = query.Where(t => t.Title.Contains(title));
        }

        if (!string.IsNullOrWhiteSpace(model.Author))
        {
            var author = model.Author.Trim();
            query = query.Where(t => t.Author.Contains(author));
        }

        if (!string.IsNullOrWhiteSpace(model.Genre))
        {
            var genre = model.Genre.Trim();
            query = query.Where(t => t.Genre == genre);
        }

        if (model.AvailableOnly)
        {
            query = query.Where(t => t.Copies.Any(c =>
                c.CopyType == CopyType.Borrowable && c.Availability == CopyAvailability.Available));
        }

        query = query.OrderBy(t => t.Title);

        var totalCount = await query.CountAsync();
        var titles = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<InquiryResultViewModel>
        {
            Items = titles.Select(BuildInquiryResult).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<CatalogueFacets> GetCatalogueFacetsAsync()
    {
        var genres = await _db.BookTitles
            .Where(t => t.Genre != null)
            .Select(t => t.Genre!)
            .Distinct()
            .OrderBy(g => g)
            .ToListAsync();

        var titles = await _db.BookTitles
            .Select(t => t.Title)
            .Distinct()
            .OrderBy(t => t)
            .Take(200)
            .ToListAsync();

        var authors = await _db.BookTitles
            .Select(t => t.Author)
            .Distinct()
            .OrderBy(a => a)
            .Take(200)
            .ToListAsync();

        return new CatalogueFacets
        {
            Genres = genres,
            TitleSuggestions = titles,
            AuthorSuggestions = authors
        };
    }

    private static InquiryResultViewModel BuildInquiryResult(BookTitle title)
    {
        var copies = title.Copies.ToList();
        var borrowable = copies.Where(c => c.CopyType == CopyType.Borrowable).ToList();
        var reference = copies.Count(c => c.CopyType == CopyType.Reference);
        var available = borrowable.Count(c => c.Availability == CopyAvailability.Available);
        var onLoan = borrowable.Count(c => c.Availability == CopyAvailability.OnLoan);
        var reserved = borrowable.Count(c => c.Availability == CopyAvailability.AwaitingPickup);
        var lost = copies.Count(c => c.Availability == CopyAvailability.Lost);
        var withdrawn = copies.Count(c => c.Availability == CopyAvailability.Withdrawn);

        // "In circulation" excludes copies that have been lost or withdrawn — those are no
        // longer part of the active lending pool even though the row still exists for history.
        var activeBorrowable = available + onLoan + reserved;

        var summary = BuildSummary(activeBorrowable, reference, available, onLoan, reserved, lost, withdrawn);

        return new InquiryResultViewModel
        {
            BookNumber = title.BookNumber,
            Title = title.Title,
            Author = title.Author,
            Publisher = title.Publisher,
            Classification = title.Classification,
            Genre = title.Genre,
            Description = title.Description,
            CoverImageUrl = title.CoverImageUrl,
            TotalCopies = copies.Count,
            AvailableCopies = available,
            OnLoanCopies = onLoan,
            ReservedCopies = reserved,
            ReferenceCopies = reference,
            LostCopies = lost,
            WithdrawnCopies = withdrawn,
            Summary = summary,
            CopyDetails = copies.Select(c => new CopyStatusDetail
            {
                Id = c.Id,
                AccessionNumber = c.AccessionNumber,
                CopyType = c.CopyType,
                Availability = c.Availability,
                StatusDescription = DescribeCopyStatus(c)
            }).OrderBy(c => c.AccessionNumber).ToList()
        };
    }

    private static string BuildSummary(int activeBorrowable, int reference, int available, int onLoan, int reserved, int lost, int withdrawn)
    {
        if (reference > 0 && activeBorrowable == 0)
        {
            return "Reference only — not borrowable.";
        }

        if (activeBorrowable == 0)
        {
            return lost + withdrawn > 0
                ? "No copies currently in circulation (lost or withdrawn)."
                : "No borrowable copies in catalogue.";
        }

        var suffix = lost + withdrawn > 0
            ? $" ({lost + withdrawn} lost/withdrawn, not counted below.)"
            : string.Empty;

        if (available == activeBorrowable)
        {
            return "All borrowable copies are available." + suffix;
        }

        if (available == 0 && onLoan == activeBorrowable)
        {
            return (reserved > 0
                ? "All borrowable copies are on loan; some are awaiting pickup by reservers."
                : "All borrowable copies are currently on loan.") + suffix;
        }

        if (available > 0 && onLoan > 0)
        {
            return $"Some copies available ({available}), some on loan ({onLoan})." + suffix;
        }

        if (reserved > 0)
        {
            return $"{reserved} copy/copies awaiting pickup for reservations." + suffix;
        }

        return "Mixed availability — see copy details." + suffix;
    }

    private static string DescribeCopyStatus(BookCopy copy)
    {
        if (copy.Availability == CopyAvailability.Lost)
        {
            return "Lost";
        }

        if (copy.Availability == CopyAvailability.Withdrawn)
        {
            return "Withdrawn from circulation";
        }

        if (copy.CopyType == CopyType.Reference)
        {
            return "Reference only (not borrowable)";
        }

        return copy.Availability switch
        {
            CopyAvailability.Available => "Available for loan",
            CopyAvailability.OnLoan => "Currently on loan",
            CopyAvailability.AwaitingPickup => "Awaiting pickup (reserved)",
            _ => copy.Availability.ToString()
        };
    }
}
