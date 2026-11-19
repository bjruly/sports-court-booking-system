using System.ComponentModel.DataAnnotations;

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

public class CustomerReviewRequest
{
    [Range(1, 5, ErrorMessage = "Vui lòng chọn số sao từ 1 đến 5.")]
    public int Rating { get; set; }

    [StringLength(500, ErrorMessage = "Nhận xét không được vượt quá 500 ký tự.")]
    public string? Comment { get; set; }
}

public class CustomerReviewFormDto
{
    public int BookingId { get; set; }
    public string CourtName { get; set; } = string.Empty;
    public DateOnly BookingDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int Rating { get; set; } = 5;
    public string? Comment { get; set; }
    public bool IsEditing { get; set; }
}

public class OwnerReviewDto
{
    public int Id { get; set; }
    public int? BookingId { get; set; }
    public string CourtName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateOnly? BookingDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? OwnerReply { get; set; }
    public DateTime? OwnerRepliedAt { get; set; }
}
