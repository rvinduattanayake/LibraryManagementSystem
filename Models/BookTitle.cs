using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

/// <summary>
/// A Title represents the class of all identical books (e.g. same ISBN/edition).
/// Physical items on the shelf are BookCopy records linked to this title.
/// </summary>
public class BookTitle
{
    public int Id { get; set; }

    /// <summary>Book number: classification code + sequence (e.g. 005.5-0001).</summary>
    [Required, MaxLength(20)]
    public string BookNumber { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Author { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Isbn { get; set; }

    [Required, MaxLength(100)]
    public string Classification { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Publisher { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Genre { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? CoverImageUrl { get; set; }

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    public ICollection<BookCopy> Copies { get; set; } = new List<BookCopy>();
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
