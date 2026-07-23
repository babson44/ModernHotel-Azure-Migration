using Microsoft.AspNetCore.Mvc;
using ModernHotel.Core.DTOs;
using ModernHotel.Web.Services;

namespace ModernHotel.Web.Controllers;

public class GuestsController : Controller
{
    private readonly HotelApiClient _api;
    public GuestsController(HotelApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var guests = await _api.GetGuestsAsync() ?? new();
        return View(guests);
    }

    public IActionResult Create() => View(new CreateGuestDto("", "", "", "", null, null));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateGuestDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        await _api.CreateGuestAsync(dto);
        return RedirectToAction(nameof(Index));
    }
}
