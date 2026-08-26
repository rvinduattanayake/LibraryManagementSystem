namespace LibraryManagementSystem.Models;

public class Reservation
{
    public int Id { get; set; }

    public int BorrowerId { get; set; }
    public Borrower Borrower { get; set; } = null!;

    public int BookTitleId { get; set; }
    public BookTitle BookTitle { get; set; } = null!;

    public DateTime ReservedAt { get; set; } = DateTime.UtcNow;
}
