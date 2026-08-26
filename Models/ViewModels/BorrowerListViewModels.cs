using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models.ViewModels;

public class BorrowerListItemViewModel
{
    public int Id { get; set; }
    public string UserNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NationalIdNumber { get; set; } = string.Empty;
}

public class BorrowerEditViewModel
{
    public int Id { get; set; }

    [Display(Name = "User Number")]
    public string UserNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name is required."), StringLength(150, ErrorMessage = "Name must be at most 150 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required]
    public Sex Sex { get; set; }

    [Display(Name = "National ID Number")]
    public string NationalIdNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Address is required."), StringLength(300, ErrorMessage = "Address must be at most 300 characters.")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required."), EmailAddress(ErrorMessage = "Enter a valid email address."), StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Enter a valid phone number."), StringLength(30)]
    public string? Phone { get; set; }
}
