using System.Security.Claims;
using EventMaster.Application.DTOs.Seats;
using EventMaster.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventMaster.Web.Controllers;

[Authorize]
public class SeatController : Controller
{
    private readonly ISeatService _seats;

    public SeatController(ISeatService seats)
    {
        _seats = seats;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [Route("/Seat/Event/{id:guid}")]
    public async Task<IActionResult> Event(Guid id, CancellationToken cancellationToken)
    {
        var result = await _seats.GetEventSeatMapAsync(id, CurrentUserId, cancellationToken);
        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Hold(HoldSeatRequest request, CancellationToken cancellationToken)
    {
        var r = await _seats.HoldSeatAsync(request.EventId, request.RoomSeatId, CurrentUserId, cancellationToken);
        return Json(r);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Release(HoldSeatRequest request, CancellationToken cancellationToken)
    {
        var r = await _seats.ReleaseSeatAsync(request.EventId, request.RoomSeatId, CurrentUserId, cancellationToken);
        return Json(r);
    }
}

