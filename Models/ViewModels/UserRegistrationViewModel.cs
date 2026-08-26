using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models.ViewModels;

public class UserRegistrationViewModel
{
    [Required(ErrorMessage = "User number is required."), StringLength(20, ErrorMessage = "User number must be at most 20 characters.")]
    [RegularExpression(@"^[A-Za-z0-9\-]+$", ErrorMessage = "User number may contain only letters, numbers, and hyphens.")]
    [Display(Name = "User Number")]
    public string UserNumber { get; set; } = string.Empty;

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
}
