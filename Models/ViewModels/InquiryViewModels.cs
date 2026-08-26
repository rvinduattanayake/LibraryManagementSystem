using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models.ViewModels;

public class InquirySearchViewModel
{
    [Display(Name = "Accession Number")]
    public string? AccessionNumber { get; set; }

    [Display(Name = "Title (partial or full)")]
    public string? Title { get; set; }

    [Display(Name = "Author (partial or full)")]
    public string? Author { get; set; }

    [Display(Name = "Genre")]
    public string? Genre { get; set; }

    [Display(Name = "Available now only")]
    public bool AvailableOnly { get; set; }
}

public class CatalogueFacets
{
    public List<string> Genres { get; set; } = new();
    public List<string> TitleSuggestions { get; set; } = new();
    public List<string> AuthorSuggestions { get; set; } = new();
}

public class CopyStatusDetail
{
    public int Id { get; set; }
    public string AccessionNumber { get; set; } = string.Empty;
    public CopyType CopyType { get; set; }
    public CopyAvailability Availability { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
}

public class InquiryResultViewModel
{
    public string BookNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string Classification { get; set; } = string.Empty;
    public string? Genre { get; set; }
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public int OnLoanCopies { get; set; }
    public int ReservedCopies { get; set; }
    public int ReferenceCopies { get; set; }
    public int LostCopies { get; set; }
    public int WithdrawnCopies { get; set; }
    public string Summary { get; set; } = string.Empty;
    public List<CopyStatusDetail> CopyDetails { get; set; } = new();
}
