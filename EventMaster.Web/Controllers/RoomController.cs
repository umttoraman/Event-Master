using EventMaster.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventMaster.Web.Controllers;

[Authorize]
public class RoomController : Controller
{
    private readonly IRoomService _rooms;

    public RoomController(IRoomService rooms)
    {
        _rooms = rooms;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var list = await _rooms.GetRoomsWithAvailabilityAsync(cancellationToken);
        return View(list);
    }
}
