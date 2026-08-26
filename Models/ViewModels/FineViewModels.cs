namespace LibraryManagementSystem.Models.ViewModels;

public class FineListItemViewModel
{
    public int LoanId { get; set; }
    public string BorrowerName { get; set; } = string.Empty;
    public string UserNumber { get; set; } = string.Empty;
    public string BookTitle { get; set; } = string.Empty;
    public string AccessionNumber { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public DateTime ReturnedAt { get; set; }
    public decimal FineAmount { get; set; }
    public bool IsPaid { get; set; }
}
