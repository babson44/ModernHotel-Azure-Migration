namespace ModernHotel.Core.Entities;

public class Reservation
{
    public int Id { get; set; }
    public int GuestId { get; set; }
    public int RoomId { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    public decimal TotalAmount { get; set; }
    public int Adults { get; set; } = 1;
    public int Children { get; set; } = 0;
    public string? SpecialRequests { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ActualCheckIn { get; set; }
    public DateTime? ActualCheckOut { get; set; }

    public Guest Guest { get; set; } = null!;
    public Room Room { get; set; } = null!;

    public int Nights => (CheckOutDate - CheckInDate).Days;
}

public enum ReservationStatus
{
    Pending = 1,
    Confirmed = 2,
    CheckedIn = 3,
    CheckedOut = 4,
    Cancelled = 5,
    NoShow = 6
}
