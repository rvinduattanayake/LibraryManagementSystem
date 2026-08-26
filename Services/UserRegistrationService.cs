using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services;

public interface IUserRegistrationService
{
    Task<OperationResult<Borrower>> RegisterUserAsync(UserRegistrationViewModel model);
    Task<OperationResult<Borrower>> RegisterMemberAsync(MemberRegisterViewModel model, string applicationUserId);
    Task<Borrower?> GetBorrowerByApplicationUserIdAsync(string applicationUserId);
    Task<PagedResult<BorrowerListItemViewModel>> GetAllBorrowersAsync(int page = 1, int pageSize = LibraryConstants.DefaultPageSize);
    Task<BorrowerEditViewModel?> GetBorrowerForEditAsync(int id);
    Task<OperationResult> UpdateBorrowerAsync(BorrowerEditViewModel model);
}

public class UserRegistrationService : IUserRegistrationService
{
    private readonly LibraryDbContext _db;

    public UserRegistrationService(LibraryDbContext db)
    {
        _db = db;
    }

    public async Task<OperationResult<Borrower>> RegisterUserAsync(UserRegistrationViewModel model)
    {
        if (await _db.Borrowers.AnyAsync(b => b.UserNumber == model.UserNumber.Trim()))
        {
            return OperationResult<Borrower>.Fail($"User number '{model.UserNumber}' is already registered.");
        }

        if (await _db.Borrowers.AnyAsync(b => b.NationalIdNumber == model.NationalIdNumber.Trim()))
        {
            return OperationResult<Borrower>.Fail("A borrower with this national ID number already exists.");
        }

        var borrower = new Borrower
        {
            UserNumber = model.UserNumber.Trim(),
            Name = model.Name.Trim(),
            Sex = model.Sex,
            NationalIdNumber = model.NationalIdNumber.Trim(),
            Address = model.Address.Trim(),
            Email = model.Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim()
        };

        _db.Borrowers.Add(borrower);
        await _db.SaveChangesAsync();

        return OperationResult<Borrower>.Ok(borrower, $"Borrower {borrower.Name} registered with user number {borrower.UserNumber}.");
    }

    public async Task<OperationResult<Borrower>> RegisterMemberAsync(MemberRegisterViewModel model, string applicationUserId)
    {
        if (await _db.Borrowers.AnyAsync(b => b.NationalIdNumber == model.NationalIdNumber.Trim()))
        {
            return OperationResult<Borrower>.Fail(
                "A borrower with this national ID number is already registered. If you already have a library card, visit the front desk to link it to your online account.");
        }

        if (await _db.Borrowers.AnyAsync(b => b.ApplicationUserId == applicationUserId))
        {
            return OperationResult<Borrower>.Fail("This account is already linked to a library card.");
        }

        var sequence = await _db.Borrowers.CountAsync() + 1;
        var userNumber = $"M-{sequence:D5}";

        var borrower = new Borrower
        {
            UserNumber = userNumber,
            Name = model.Name.Trim(),
            Sex = model.Sex,
            NationalIdNumber = model.NationalIdNumber.Trim(),
            Address = model.Address.Trim(),
            Email = model.Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim(),
            ApplicationUserId = applicationUserId
        };

        _db.Borrowers.Add(borrower);
        await _db.SaveChangesAsync();

        return OperationResult<Borrower>.Ok(borrower, $"Welcome, {borrower.Name}! Your library card number is {borrower.UserNumber}.");
    }

    public Task<Borrower?> GetBorrowerByApplicationUserIdAsync(string applicationUserId) =>
        _db.Borrowers.FirstOrDefaultAsync(b => b.ApplicationUserId == applicationUserId);

    public async Task<PagedResult<BorrowerListItemViewModel>> GetAllBorrowersAsync(int page = 1, int pageSize = LibraryConstants.DefaultPageSize)
    {
        page = Math.Max(1, page);
        var query = _db.Borrowers.OrderBy(b => b.Name);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BorrowerListItemViewModel
            {
                Id = b.Id,
                UserNumber = b.UserNumber,
                Name = b.Name,
                NationalIdNumber = b.NationalIdNumber
            })
            .ToListAsync();

        return new PagedResult<BorrowerListItemViewModel>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<BorrowerEditViewModel?> GetBorrowerForEditAsync(int id)
    {
        var borrower = await _db.Borrowers.FindAsync(id);
        if (borrower is null)
        {
            return null;
        }

        return new BorrowerEditViewModel
        {
            Id = borrower.Id,
            UserNumber = borrower.UserNumber,
            Name = borrower.Name,
            Sex = borrower.Sex,
            NationalIdNumber = borrower.NationalIdNumber,
            Address = borrower.Address,
            Email = borrower.Email,
            Phone = borrower.Phone
        };
    }

    public async Task<OperationResult> UpdateBorrowerAsync(BorrowerEditViewModel model)
    {
        var borrower = await _db.Borrowers.FindAsync(model.Id);
        if (borrower is null)
        {
            return OperationResult.Fail("Borrower not found.");
        }

        borrower.Name = model.Name.Trim();
        borrower.Sex = model.Sex;
        borrower.Address = model.Address.Trim();
        borrower.Email = model.Email.Trim();
        borrower.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();

        await _db.SaveChangesAsync();
        return OperationResult.Ok($"Borrower {borrower.Name} updated.");
    }
}
