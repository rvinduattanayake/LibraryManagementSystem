namespace LibraryManagementSystem.Models;

public static class LibraryConstants
{
    public const int MaxBooksPerBorrower = 5;
    public const int LoanPeriodDays = 14;
    public const int MaxCopiesPerTitle = 10;
    public const int PickupWindowDays = 3;
    public const int DefaultPageSize = 10;
    public const decimal FinePerDayOverdue = 0.50m;
    public const int DueSoonReminderDays = 2;
}
