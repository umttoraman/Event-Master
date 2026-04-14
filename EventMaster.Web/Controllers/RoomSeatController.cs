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

        var selectedRoom = rooms.FirstOrDefault(x => x.Id == selectedRoomId);
        var rows = selectedRoom is null ? 10 : (int)Math.Ceiling(selectedRoom.Capacity / 10m);
        if (rows <= 0) rows = 1;

        return View(new GenerateRoomSeatsRequest
        {
            RoomId = selectedRoomId,
            Rows = rows,
            SeatsPerRow = 10
        });
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

