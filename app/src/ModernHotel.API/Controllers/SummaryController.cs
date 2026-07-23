using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ModernHotel.Core.DTOs;
using ModernHotel.Core.Entities;
using ModernHotel.Infrastructure.Data;

namespace ModernHotel.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SummaryController : ControllerBase
{
    private readonly HotelDbContext _db;

    public SummaryController(HotelDbContext db) => _db = db;

    [HttpGet]
    public async Task<DashboardSummaryDto> GetSummary()
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var totalGuests = await _db.Guests.CountAsync();
        var totalRooms = await _db.Rooms.CountAsync(r => r.IsActive);

        var occupiedRoomIds = await _db.Reservations
            .Where(r => r.CheckInDate.Date <= today && r.CheckOutDate.Date > today &&
                        (r.Status == ReservationStatus.CheckedIn || r.Status == ReservationStatus.Confirmed))
            .Select(r => r.RoomId)
            .Distinct()
            .ToListAsync();

        var availableRooms = totalRooms - occupiedRoomIds.Count;
        var activeReservations = await _db.Reservations
            .CountAsync(r => r.Status == ReservationStatus.CheckedIn || r.Status == ReservationStatus.Confirmed);

        var arrivalsToday = await _db.Reservations
            .CountAsync(r => r.CheckInDate.Date == today && r.Status == ReservationStatus.Confirmed);

        var departuresToday = await _db.Reservations
            .CountAsync(r => r.CheckOutDate.Date == today && r.Status == ReservationStatus.CheckedIn);

        var checkedInGuests = await _db.Reservations
            .CountAsync(r => r.Status == ReservationStatus.CheckedIn);

        var occupancyRate = totalRooms > 0
            ? Math.Round((decimal)occupiedRoomIds.Count / totalRooms * 100, 1)
            : 0m;

        var revenueThisMonth = await _db.Reservations
            .Where(r => r.CreatedAt >= monthStart &&
                        (r.Status == ReservationStatus.CheckedIn ||
                         r.Status == ReservationStatus.CheckedOut ||
                         r.Status == ReservationStatus.Confirmed))
            .SumAsync(r => r.TotalAmount);

        return new DashboardSummaryDto(
            totalGuests, totalRooms, availableRooms, activeReservations,
            arrivalsToday, departuresToday, checkedInGuests,
            occupancyRate, revenueThisMonth);
    }
}
