using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

public class Borrower
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string UserNumber { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public Sex Sex { get; set; }

    [Required, MaxLength(30)]
    public string NationalIdNumber { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string Address { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? Phone { get; set; }

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Links this borrower to a self-service login, if they registered themselves online rather
    /// than being registered by a librarian at the desk. Null for desk-registered borrowers.
    /// </summary>
    [MaxLength(450)]
    public string? ApplicationUserId { get; set; }

    public ApplicationUser? ApplicationUser { get; set; }

    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<LibraryNotification> Notifications { get; set; } = new List<LibraryNotification>();
}
