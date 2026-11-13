namespace SportsCourtBooking.Domain.Entities;

public class Review : BaseEntity
{
    public int UserId { get; set; }
    public int CourtId { get; set; }
    public int Rating { get; set; } // Từ 1 đến 5 sao
    public string Comment { get; set; } = string.Empty;

    // Navigation properties
    public User? User { get; set; }
    public Court? Court { get; set; }
}