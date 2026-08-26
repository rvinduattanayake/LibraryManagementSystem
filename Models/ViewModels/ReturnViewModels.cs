using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models.ViewModels;

public class ReturnRequestViewModel
{
    [Required, Display(Name = "Accession Number")]
    public string AccessionNumber { get; set; } = string.Empty;
}

public class ReturnResultViewModel
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ReservationNotice { get; set; }
    public string? NotifiedBorrower { get; set; }
}
