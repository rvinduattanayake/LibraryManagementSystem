using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

/// <summary>
/// A Copy is a physical book on the shelf. Accession number = book number + copy suffix.
/// </summary>
public class BookCopy
{
    public int Id { get; set; }

    public int BookTitleId { get; set; }
    public BookTitle BookTitle { get; set; } = null!;

    /// <summary>Accession number (e.g. 005.5-0001-01).</summary>
    [Required, MaxLength(25)]
    public string AccessionNumber { get; set; } = string.Empty;

    public int CopyNumber { get; set; }

    public CopyType CopyType { get; set; } = CopyType.Borrowable;

    public CopyAvailability Availability { get; set; } = CopyAvailability.Available;

    public int? AwaitingBorrowerId { get; set; }
    public Borrower? AwaitingBorrower { get; set; }

    /// <summary>When an AwaitingPickup hold expires and should be released to the next reservation (or back to Available).</summary>
    public DateTime? AwaitingPickupExpiresAt { get; set; }

    /// <summary>Optimistic concurrency token — bumped on every availability change to guard against double-allocation races.</summary>
    [ConcurrencyCheck]
    public int Version { get; set; }

    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
}
