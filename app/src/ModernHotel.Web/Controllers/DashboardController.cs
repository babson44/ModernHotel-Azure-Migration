using Microsoft.AspNetCore.Mvc;
using ModernHotel.Web.Services;

namespace ModernHotel.Web.Controllers;

public class DashboardController : Controller
{
    private readonly HotelApiClient _api;
    public DashboardController(HotelApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var summary = await _api.GetSummaryAsync();
        return View(summary);
    }
}
