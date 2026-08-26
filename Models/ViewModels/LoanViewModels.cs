using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models.ViewModels;

public class LoanRequestViewModel
{
    [Required(ErrorMessage = "Borrower user number is required."), Display(Name = "Borrower User Number")]
    public string UserNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter at least one accession number.")]
    [StringLength(2000, ErrorMessage = "Accession number list must be at most {1} characters.")]
    [Display(Name = "Accession Numbers (one per line)")]
    public string AccessionNumbers { get; set; } = string.Empty;
}

public class LoanActionViewModel
{
    public int LoanId { get; set; }
    public string BorrowerName { get; set; } = string.Empty;
    public string AccessionNumber { get; set; } = string.Empty;
    public string BookTitle { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public LoanStatus Status { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal FineAmount { get; set; }
}

public class LoanResultViewModel
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<LoanActionViewModel> ConfirmedLoans { get; set; } = new();
}
