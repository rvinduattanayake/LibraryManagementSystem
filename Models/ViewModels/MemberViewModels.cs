using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models.ViewModels;

public class MemberRegisterViewModel
{
    [Required(ErrorMessage = "Name is required."), StringLength(150, ErrorMessage = "Name must be at most 150 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required]
    public Sex Sex { get; set; }

    [Required(ErrorMessage = "National ID number is required."), StringLength(30, ErrorMessage = "National ID number must be at most 30 characters.")]
    [Display(Name = "National ID Number")]
    public string NationalIdNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Address is required."), StringLength(300, ErrorMessage = "Address must be at most 300 characters.")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required."), EmailAddress(ErrorMessage = "Enter a valid email address."), StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Enter a valid phone number."), StringLength(30)]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least {2} characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class MemberLoanRequestViewModel
{
    [Required(ErrorMessage = "Please enter at least one accession number.")]
    [StringLength(2000, ErrorMessage = "Accession number list must be at most {1} characters.")]
    [Display(Name = "Accession Numbers (one per line)")]
    public string AccessionNumbers { get; set; } = string.Empty;
}

public class MemberReservationRequestViewModel
{
    [Required(ErrorMessage = "Enter a book number, title, or author."), Display(Name = "Book Number or Title")]
    public string BookSearch { get; set; } = string.Empty;
}
