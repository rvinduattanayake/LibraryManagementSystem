namespace LibraryManagementSystem.Models;

public enum Sex
{
    Male,
    Female,
    Other
}

public enum CopyType
{
    Borrowable,
    Reference
}

public enum CopyAvailability
{
    Available,
    OnLoan,
    AwaitingPickup,
    Lost,
    Withdrawn
}

public enum LoanStatus
{
    Pending,
    Active,
    Returned,
    Cancelled
}
