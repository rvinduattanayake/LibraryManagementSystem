namespace LibraryManagementSystem.Models;

public class LibraryNotification
{
    public int Id { get; set; }

    public int BorrowerId { get; set; }
    public Borrower Borrower { get; set; } = null!;

    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsRead { get; set; }
}
