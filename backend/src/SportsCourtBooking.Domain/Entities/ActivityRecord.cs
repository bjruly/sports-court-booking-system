using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Domain.Entities;

public sealed class ActivityRecord
{
    public long Id { get; set; }
    public int ActorId { get; init; }
    public UserRole ActorRole { get; init; }
    public required string Action { get; init; }
    public required string Target { get; init; }
    public DateTime OccurredAtUtc { get; init; }
}
