using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Data;

public class LibraryDbContext : IdentityDbContext<ApplicationUser>
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options)
    {
    }

    public DbSet<BookTitle> BookTitles => Set<BookTitle>();
    public DbSet<BookCopy> BookCopies => Set<BookCopy>();
    public DbSet<Borrower> Borrowers => Set<Borrower>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<LibraryNotification> Notifications => Set<LibraryNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BookTitle>()
            .HasIndex(t => t.BookNumber)
            .IsUnique();

        modelBuilder.Entity<BookCopy>()
            .HasIndex(c => c.AccessionNumber)
            .IsUnique();

        modelBuilder.Entity<Borrower>()
            .HasIndex(b => b.UserNumber)
            .IsUnique();

        modelBuilder.Entity<Borrower>()
            .HasIndex(b => b.NationalIdNumber)
            .IsUnique();

        modelBuilder.Entity<BookCopy>()
            .HasOne(c => c.AwaitingBorrower)
            .WithMany()
            .HasForeignKey(c => c.AwaitingBorrowerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Borrower>()
            .HasIndex(b => b.ApplicationUserId)
            .IsUnique();

        modelBuilder.Entity<Borrower>()
            .HasOne(b => b.ApplicationUser)
            .WithOne()
            .HasForeignKey<Borrower>(b => b.ApplicationUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
