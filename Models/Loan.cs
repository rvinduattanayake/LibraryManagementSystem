namespace LibraryManagementSystem.Models;

public class Loan
{
    public int Id { get; set; }

    public int BorrowerId { get; set; }
    public Borrower Borrower { get; set; } = null!;

    public int BookCopyId { get; set; }
    public BookCopy BookCopy { get; set; } = null!;

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LoanDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ReturnedAt { get; set; }

    public LoanStatus Status { get; set; } = LoanStatus.Pending;

    /// <summary>Overdue fine calculated at return time (LibraryConstants.FinePerDayOverdue per day late).</summary>
    public decimal FineAmount { get; set; }

    public DateTime? FinePaidAt { get; set; }

    /// <summary>Set once a due-soon/overdue reminder notification has been sent, so it is only sent once.</summary>
    public DateTime? ReminderSentAt { get; set; }
}
