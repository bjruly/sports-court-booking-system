namespace SportsCourtBooking.Application.DTOs;

public class NotificationItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string TimeAgo { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Type { get; set; } = "info"; // "booking", "facility", "review", "system"
    public string Url { get; set; } = "#";
    public bool IsRead { get; set; }
}

public class NotificationResponseDto
{
    public bool Success { get; set; } = true;
    public int UnreadCount { get; set; }
    public List<NotificationItemDto> Items { get; set; } = new();
}
