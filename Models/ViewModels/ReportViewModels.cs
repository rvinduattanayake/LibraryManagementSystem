namespace LibraryManagementSystem.Models.ViewModels;

public class OverdueLoanViewModel
{
    public int LoanId { get; set; }
    public string BorrowerName { get; set; } = string.Empty;
    public string UserNumber { get; set; } = string.Empty;
    public string BookTitle { get; set; } = string.Empty;
    public string AccessionNumber { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public int DaysOverdue { get; set; }
}

public class PopularTitleViewModel
{
    public string Title { get; set; } = string.Empty;
    public string BookNumber { get; set; } = string.Empty;
    public int LoanCount { get; set; }
}

public class ReservationBacklogItemViewModel
{
    public string Title { get; set; } = string.Empty;
    public string BookNumber { get; set; } = string.Empty;
    public int ReservationCount { get; set; }
}

public class ReportsViewModel
{
    public List<OverdueLoanViewModel> OverdueLoans { get; set; } = new();
    public int ActiveLoanCount { get; set; }
    public decimal TotalOutstandingFines { get; set; }
    public int OutstandingFineCount { get; set; }
    public List<PopularTitleViewModel> PopularTitles { get; set; } = new();
    public List<ReservationBacklogItemViewModel> ReservationBacklog { get; set; } = new();
    public int TotalActiveReservations { get; set; }
}
