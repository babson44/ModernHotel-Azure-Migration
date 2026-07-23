namespace ModernHotel.Core.Entities;

public class Room
{
    public int Id { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public RoomType Type { get; set; }
    public int Floor { get; set; }
    public int BedCount { get; set; }
    public int MaxOccupancy { get; set; }
    public decimal RatePerNight { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}

public enum RoomType
{
    Standard = 1,
    Deluxe = 2,
    Suite = 3,
    Penthouse = 4
}
