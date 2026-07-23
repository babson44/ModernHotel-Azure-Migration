using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModernHotel.Core.DTOs;

namespace ModernHotel.Web.Services;

public class HotelApiClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };

    public HotelApiClient(HttpClient http) => _http = http;

    public Task<DashboardSummaryDto?> GetSummaryAsync() =>
        _http.GetFromJsonAsync<DashboardSummaryDto>("api/summary", _jsonOptions);

    public Task<List<GuestDto>?> GetGuestsAsync() =>
        _http.GetFromJsonAsync<List<GuestDto>>("api/guests", _jsonOptions);

    public Task<GuestDto?> GetGuestAsync(int id) =>
        _http.GetFromJsonAsync<GuestDto>($"api/guests/{id}", _jsonOptions);

    public Task<List<RoomDto>?> GetRoomsAsync(bool? availableOnly = null)
    {
        var url = availableOnly.HasValue ? $"api/rooms?availableOnly={availableOnly}" : "api/rooms";
        return _http.GetFromJsonAsync<List<RoomDto>>(url, _jsonOptions);
    }

    public Task<List<ReservationDto>?> GetReservationsAsync(string? status = null)
    {
        var url = status != null ? $"api/reservations?status={status}" : "api/reservations";
        return _http.GetFromJsonAsync<List<ReservationDto>>(url, _jsonOptions);
    }

    public Task<ReservationDto?> GetReservationAsync(int id) =>
        _http.GetFromJsonAsync<ReservationDto>($"api/reservations/{id}", _jsonOptions);

    public async Task<bool> CheckInAsync(int id)
    {
        var response = await _http.PatchAsync($"api/reservations/{id}/checkin", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> CheckOutAsync(int id)
    {
        var response = await _http.PatchAsync($"api/reservations/{id}/checkout", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> CancelAsync(int id)
    {
        var response = await _http.PatchAsync($"api/reservations/{id}/cancel", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<ReservationDto?> CreateReservationAsync(CreateReservationDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/reservations", dto, _jsonOptions);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ReservationDto>(_jsonOptions);
    }

    public async Task<GuestDto?> CreateGuestAsync(CreateGuestDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/guests", dto, _jsonOptions);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<GuestDto>(_jsonOptions);
    }
}
