using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ModernHotel.Core.DTOs;
using ModernHotel.Infrastructure.Data;

namespace ModernHotel.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController : ControllerBase
{
    private readonly HotelDbContext _db;

    public RoomsController(HotelDbContext db) => _db = db;

    [HttpGet]
    public async Task<IEnumerable<RoomDto>> GetAll([FromQuery] bool? availableOnly = null)
    {
        var today = DateTime.UtcNow.Date;
        var query = _db.Rooms.AsQueryable();

        if (availableOnly == true)
        {
            query = query.Where(r => r.IsActive &&
                !r.Reservations.Any(res =>
                    res.CheckInDate.Date <= today &&
                    res.CheckOutDate.Date > today &&
                    (res.Status == Core.Entities.ReservationStatus.CheckedIn ||
                     res.Status == Core.Entities.ReservationStatus.Confirmed)));
        }

        return await query
            .OrderBy(r => r.RoomNumber)
            .Select(r => new RoomDto(
                r.Id, r.RoomNumber, r.Type, r.Type.ToString(), r.Floor,
                r.BedCount, r.MaxOccupancy, r.RatePerNight, r.Description, r.IsActive,
                r.IsActive && !r.Reservations.Any(res =>
                    res.CheckInDate.Date <= today &&
                    res.CheckOutDate.Date > today &&
                    (res.Status == Core.Entities.ReservationStatus.CheckedIn ||
                     res.Status == Core.Entities.ReservationStatus.Confirmed))
            ))
            .ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoomDto>> GetById(int id)
    {
        var today = DateTime.UtcNow.Date;
        var r = await _db.Rooms.Include(r => r.Reservations).FirstOrDefaultAsync(r => r.Id == id);
        if (r == null) return NotFound();

        var isAvailableToday = r.IsActive && !r.Reservations.Any(res =>
            res.CheckInDate.Date <= today && res.CheckOutDate.Date > today &&
            (res.Status == Core.Entities.ReservationStatus.CheckedIn ||
             res.Status == Core.Entities.ReservationStatus.Confirmed));

        return new RoomDto(r.Id, r.RoomNumber, r.Type, r.Type.ToString(), r.Floor,
            r.BedCount, r.MaxOccupancy, r.RatePerNight, r.Description, r.IsActive, isAvailableToday);
    }
}
