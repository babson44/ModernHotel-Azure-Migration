using ModernHotel.Core.Entities;

namespace ModernHotel.Core.DTOs;

public record RoomDto(
    int Id,
    string RoomNumber,
    RoomType Type,
    string TypeName,
    int Floor,
    int BedCount,
    int MaxOccupancy,
    decimal RatePerNight,
    string? Description,
    bool IsActive,
    bool IsAvailableToday
);
