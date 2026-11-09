namespace SportsCourtBooking.Web.Models;

public sealed class CourtCatalogViewModel
{
    public string? Sport { get; init; }

    public string? Area { get; init; }

    public DateOnly PlayDate { get; init; }

    public IReadOnlyList<CourtCardViewModel> Courts { get; init; } = [];
}

public sealed class CourtCardViewModel
{
    public int Id { get; init; }

    public string FacilityName { get; init; } = string.Empty;

    public string CourtName { get; init; } = string.Empty;

    public string Sport { get; init; } = string.Empty;

    public string District { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string OpeningHours { get; init; } = string.Empty;

    public decimal StartingPrice { get; init; }

    public string Description { get; init; } = string.Empty;

    public string VisualClass { get; init; } = string.Empty;
}

public sealed class CourtScheduleViewModel
{
    public CourtCardViewModel Court { get; init; } = new();

    public DateOnly PlayDate { get; init; }

    public IReadOnlyList<CourtSlotViewModel> Slots { get; init; } = [];

    public string? ErrorMessage { get; init; }
}

public sealed class CourtSlotViewModel
{
    public int StartHour { get; init; }

    public decimal Price { get; init; }

    public bool IsAvailable { get; init; }

    public string StatusLabel { get; init; } = string.Empty;
}

public sealed class BookingReviewViewModel
{
    public CourtCardViewModel Court { get; init; } = new();

    public DateOnly PlayDate { get; init; }

    public IReadOnlyList<CourtSlotViewModel> Slots { get; init; } = [];

    public decimal TotalPrice => Slots.Sum(slot => slot.Price);

    public string ReturnUrl { get; init; } = string.Empty;
}

public sealed class AccountPrototypeViewModel
{
    public string? ReturnUrl { get; init; }
}
