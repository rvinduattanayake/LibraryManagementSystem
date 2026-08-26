namespace LibraryManagementSystem.Models.ViewModels;

public class NotificationListItemViewModel
{
    public int Id { get; set; }
    public string BorrowerName { get; set; } = string.Empty;
    public string UserNumber { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
}
