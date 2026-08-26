using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models.ViewModels;

public class BookListItemViewModel
{
    public int Id { get; set; }
    public string BookNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int TotalCopies { get; set; }
}

public class BookEditViewModel
{
    public int Id { get; set; }

    [Display(Name = "Book Number")]
    public string BookNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Book title is required."), StringLength(300, ErrorMessage = "Title must be at most 300 characters.")]
    [Display(Name = "Book Title")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Author is required."), StringLength(200, ErrorMessage = "Author must be at most 200 characters.")]
    public string Author { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "ISBN must be at most 20 characters.")]
    [RegularExpression(@"^[0-9Xx\-]*$", ErrorMessage = "ISBN may contain only digits, hyphens, and 'X'.")]
    [Display(Name = "ISBN")]
    public string? Isbn { get; set; }

    [Required(ErrorMessage = "Publisher is required."), StringLength(150, ErrorMessage = "Publisher must be at most 150 characters.")]
    public string Publisher { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Genre must be at most 100 characters.")]
    public string? Genre { get; set; }

    [StringLength(2000, ErrorMessage = "Description must be at most 2000 characters.")]
    public string? Description { get; set; }

    [StringLength(500, ErrorMessage = "Cover image URL must be at most 500 characters.")]
    [Url(ErrorMessage = "Enter a valid URL.")]
    [Display(Name = "Cover Image URL")]
    public string? CoverImageUrl { get; set; }

    public List<CopyStatusDetail> Copies { get; set; } = new();
}
