using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models.ViewModels;

public class ReservationRequestViewModel
{
    [Required, Display(Name = "Borrower User Number")]
    public string UserNumber { get; set; } = string.Empty;

    [Required, Display(Name = "Book Number or Title")]
    public string BookSearch { get; set; } = string.Empty;
}

public class ReservationListItemViewModel
{
    public int Id { get; set; }
    public string BorrowerName { get; set; } = string.Empty;
    public string UserNumber { get; set; } = string.Empty;
    public string BookTitle { get; set; } = string.Empty;
    public string BookNumber { get; set; } = string.Empty;
    public DateTime ReservedAt { get; set; }
}
