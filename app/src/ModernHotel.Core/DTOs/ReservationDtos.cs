using ModernHotel.Core.Entities;

namespace ModernHotel.Core.DTOs;

public record ReservationDto(
    int Id,
    int GuestId,
    string GuestName,
    string GuestEmail,
    int RoomId,
    string RoomNumber,
    RoomType RoomType,
    string RoomTypeName,
    DateTime CheckInDate,
    DateTime CheckOutDate,
    int Nights,
    ReservationStatus Status,
    string StatusName,
    decimal TotalAmount,
    int Adults,
    int Children,
    string? SpecialRequests,
    DateTime CreatedAt,
    DateTime? ActualCheckIn,
    DateTime? ActualCheckOut
);

public record CreateReservationDto(
    int GuestId,
    int RoomId,
    DateTime CheckInDate,
    DateTime CheckOutDate,
    int Adults,
    int Children,
    string? SpecialRequests
);
