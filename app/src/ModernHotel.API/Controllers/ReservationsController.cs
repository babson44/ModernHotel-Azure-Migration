using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ModernHotel.Core.DTOs;
using ModernHotel.Core.Entities;
using ModernHotel.Infrastructure.Data;

namespace ModernHotel.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationsController : ControllerBase
{
    private readonly HotelDbContext _db;

    public ReservationsController(HotelDbContext db) => _db = db;

    [HttpGet]
    public async Task<IEnumerable<ReservationDto>> GetAll([FromQuery] string? status = null)
    {
        var query = _db.Reservations
            .Include(r => r.Guest)
            .Include(r => r.Room)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<ReservationStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(r => r.Status == parsedStatus);
        }

        return await query
            .OrderByDescending(r => r.CheckInDate)
            .Select(r => MapToDto(r))
            .ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReservationDto>> GetById(int id)
    {
        var r = await _db.Reservations
            .Include(r => r.Guest)
            .Include(r => r.Room)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (r == null) return NotFound();
        return MapToDto(r);
    }

    [HttpPost]
    public async Task<ActionResult<ReservationDto>> Create(CreateReservationDto dto)
    {
        var room = await _db.Rooms.FindAsync(dto.RoomId);
        if (room == null) return BadRequest("Room not found.");
        if (!await _db.Guests.AnyAsync(g => g.Id == dto.GuestId)) return BadRequest("Guest not found.");
        if (dto.CheckOutDate <= dto.CheckInDate) return BadRequest("Check-out must be after check-in.");

        var nights = (dto.CheckOutDate - dto.CheckInDate).Days;
        var reservation = new Reservation
        {
            GuestId = dto.GuestId,
            RoomId = dto.RoomId,
            CheckInDate = dto.CheckInDate,
            CheckOutDate = dto.CheckOutDate,
            Status = ReservationStatus.Confirmed,
            TotalAmount = room.RatePerNight * nights,
            Adults = dto.Adults,
            Children = dto.Children,
            SpecialRequests = dto.SpecialRequests,
            CreatedAt = DateTime.UtcNow
        };
        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync();

        await _db.Entry(reservation).Reference(r => r.Guest).LoadAsync();
        await _db.Entry(reservation).Reference(r => r.Room).LoadAsync();
        return CreatedAtAction(nameof(GetById), new { id = reservation.Id }, MapToDto(reservation));
    }

    [HttpPatch("{id:int}/checkin")]
    public async Task<ActionResult<ReservationDto>> CheckIn(int id)
    {
        var r = await _db.Reservations.Include(r => r.Guest).Include(r => r.Room).FirstOrDefaultAsync(r => r.Id == id);
        if (r == null) return NotFound();
        if (r.Status != ReservationStatus.Confirmed && r.Status != ReservationStatus.Pending)
            return BadRequest($"Cannot check in a reservation with status '{r.Status}'.");

        r.Status = ReservationStatus.CheckedIn;
        r.ActualCheckIn = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return MapToDto(r);
    }

    [HttpPatch("{id:int}/checkout")]
    public async Task<ActionResult<ReservationDto>> CheckOut(int id)
    {
        var r = await _db.Reservations.Include(r => r.Guest).Include(r => r.Room).FirstOrDefaultAsync(r => r.Id == id);
        if (r == null) return NotFound();
        if (r.Status != ReservationStatus.CheckedIn)
            return BadRequest($"Cannot check out a reservation with status '{r.Status}'.");

        r.Status = ReservationStatus.CheckedOut;
        r.ActualCheckOut = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return MapToDto(r);
    }

    [HttpPatch("{id:int}/cancel")]
    public async Task<ActionResult<ReservationDto>> Cancel(int id)
    {
        var r = await _db.Reservations.Include(r => r.Guest).Include(r => r.Room).FirstOrDefaultAsync(r => r.Id == id);
        if (r == null) return NotFound();
        if (r.Status == ReservationStatus.CheckedIn || r.Status == ReservationStatus.CheckedOut)
            return BadRequest($"Cannot cancel a reservation with status '{r.Status}'.");

        r.Status = ReservationStatus.Cancelled;
        await _db.SaveChangesAsync();
        return MapToDto(r);
    }

    private static ReservationDto MapToDto(Reservation r) => new(
        r.Id,
        r.GuestId,
        r.Guest.FirstName + " " + r.Guest.LastName,
        r.Guest.Email,
        r.RoomId,
        r.Room.RoomNumber,
        r.Room.Type,
        r.Room.Type.ToString(),
        r.CheckInDate,
        r.CheckOutDate,
        (r.CheckOutDate - r.CheckInDate).Days,
        r.Status,
        r.Status.ToString(),
        r.TotalAmount,
        r.Adults,
        r.Children,
        r.SpecialRequests,
        r.CreatedAt,
        r.ActualCheckIn,
        r.ActualCheckOut
    );
}
