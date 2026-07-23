using Microsoft.AspNetCore.Mvc;
using ModernHotel.Core.DTOs;
using ModernHotel.Web.Services;

namespace ModernHotel.Web.Controllers;

public class ReservationsController : Controller
{
    private readonly HotelApiClient _api;
    public ReservationsController(HotelApiClient api) => _api = api;

    public async Task<IActionResult> Index([FromQuery] string? status = null)
    {
        ViewBag.CurrentStatus = status;
        var reservations = await _api.GetReservationsAsync(status) ?? new();
        return View(reservations);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(int id)
    {
        await _api.CheckInAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckOut(int id)
    {
        await _api.CheckOutAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        await _api.CancelAsync(id);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Guests = await _api.GetGuestsAsync() ?? new();
        ViewBag.Rooms = await _api.GetRoomsAsync() ?? new();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateReservationDto dto)
    {
        await _api.CreateReservationAsync(dto);
        return RedirectToAction(nameof(Index));
    }
}
