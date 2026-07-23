using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ModernHotel.Core.DTOs;
using ModernHotel.Core.Entities;
using ModernHotel.Infrastructure.Data;

namespace ModernHotel.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GuestsController : ControllerBase
{
    private readonly HotelDbContext _db;

    public GuestsController(HotelDbContext db) => _db = db;

    [HttpGet]
    public async Task<IEnumerable<GuestDto>> GetAll()
    {
        return await _db.Guests
            .OrderBy(g => g.LastName).ThenBy(g => g.FirstName)
            .Select(g => new GuestDto(
                g.Id, g.FirstName, g.LastName, g.FirstName + " " + g.LastName,
                g.Email, g.Phone, g.Address, g.Nationality, g.CreatedAt,
                g.Reservations.Count))
            .ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GuestDto>> GetById(int id)
    {
        var g = await _db.Guests.Include(g => g.Reservations).FirstOrDefaultAsync(g => g.Id == id);
        if (g == null) return NotFound();
        return new GuestDto(g.Id, g.FirstName, g.LastName, g.FullName,
            g.Email, g.Phone, g.Address, g.Nationality, g.CreatedAt, g.Reservations.Count);
    }

    [HttpPost]
    public async Task<ActionResult<GuestDto>> Create(CreateGuestDto dto)
    {
        var guest = new Guest
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address,
            Nationality = dto.Nationality,
            CreatedAt = DateTime.UtcNow
        };
        _db.Guests.Add(guest);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = guest.Id },
            new GuestDto(guest.Id, guest.FirstName, guest.LastName, guest.FullName,
                guest.Email, guest.Phone, guest.Address, guest.Nationality, guest.CreatedAt, 0));
    }
}
