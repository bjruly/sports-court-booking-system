namespace SportsCourtBooking.Application.DTOs;

public class ReviewItemDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public int CourtId { get; set; }
    public string CourtName { get; set; } = string.Empty;
    public string CourtTypeName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    
    // UC26 fields
    public bool IsHidden { get; set; }
    public string? HiddenReason { get; set; }
    public DateTime? HiddenAt { get; set; }
    public bool IsReported { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class HideReviewRequest
{
    public int ReviewId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class ReviewStatsSummaryDto
{
    public int TotalReviews { get; set; }
    public int VisibleReviews { get; set; }
    public int ReportedReviews { get; set; }
    public int HiddenReviews { get; set; }
}
