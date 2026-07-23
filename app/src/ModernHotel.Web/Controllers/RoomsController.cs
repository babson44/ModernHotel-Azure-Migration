using Microsoft.AspNetCore.Mvc;
using ModernHotel.Web.Services;

namespace ModernHotel.Web.Controllers;

public class RoomsController : Controller
{
    private readonly HotelApiClient _api;
    public RoomsController(HotelApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var rooms = await _api.GetRoomsAsync() ?? new();
        return View(rooms);
    }
}
