namespace ModernHotel.Core.DTOs;

public record DashboardSummaryDto(
    int TotalGuests,
    int TotalRooms,
    int AvailableRooms,
    int ActiveReservations,
    int ArrivalsToday,
    int DeparturesToday,
    int CheckedInGuests,
    decimal OccupancyRate,
    decimal RevenueThisMonth
);
