using System.Security.Claims;
using EventMaster.Application.DTOs.Seats;
using EventMaster.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventMaster.Web.Controllers;

[Authorize(Roles = "Admin")]
public class RoomSeatController : Controller
{
    private readonly IRoomService _rooms;
    private readonly ISeatService _seats;

    public RoomSeatController(IRoomService rooms, ISeatService seats)
    {
        _rooms = rooms;
        _seats = seats;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Index(Guid? roomId, CancellationToken cancellationToken)
    {
        var rooms = await _rooms.GetRoomsWithAvailabilityAsync(cancellationToken);
        ViewBag.Rooms = rooms;

        var selectedRoomId = roomId ?? rooms.FirstOrDefault()?.Id ?? Guid.Empty;
        ViewBag.SelectedRoomId = selectedRoomId;

        var seats = selectedRoomId == Guid.Empty
            ? new List<RoomSeatDto>()
            : (await _seats.GetRoomSeatsAsync(selectedRoomId, cancellationToken)).ToList();

        ViewBag.Seats = seats;

        return View(new GenerateRoomSeatsRequest { RoomId = selectedRoomId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(GenerateRoomSeatsRequest model, CancellationToken cancellationToken)
    {
        var result = await _seats.GenerateRoomSeatsAsync(model, CurrentUserId, isAdmin: true, cancellationToken);
        if (!result.Success)
            TempData["Error"] = result.Error;
        else
            TempData["Message"] = $"Koltuklar oluşturuldu. ({result.Value} adet)";

        return RedirectToAction(nameof(Index), new { roomId = model.RoomId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(Guid roomSeatId, Guid roomId, bool isActive, CancellationToken cancellationToken)
    {
        var r = await _seats.SetRoomSeatActiveAsync(roomSeatId, isActive, CurrentUserId, isAdmin: true, cancellationToken);
        if (!r.Success)
            TempData["Error"] = r.Error;
        return RedirectToAction(nameof(Index), new { roomId });
    }
}

