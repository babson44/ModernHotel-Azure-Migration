namespace ModernHotel.Core.DTOs;

public record GuestDto(
    int Id,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string Phone,
    string? Address,
    string? Nationality,
    DateTime CreatedAt,
    int TotalReservations
);

public record CreateGuestDto(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string? Address,
    string? Nationality
);
