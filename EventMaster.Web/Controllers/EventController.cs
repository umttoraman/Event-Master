using System.Security.Claims;
using EventMaster.Application.DTOs.Events;
using EventMaster.Application.Services;
using EventMaster.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventMaster.Web.Controllers;

[Authorize]
public class EventController : Controller
{
    private readonly IEventService _events;
    private readonly IRoomService _rooms;
    private readonly IDocumentService _documents;

    public EventController(IEventService events, IRoomService rooms, IDocumentService documents)
    {
        _events = events;
        _rooms = rooms;
        _documents = documents;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdmin => User.IsInRole("Admin");

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var list = await _events.GetAllAsync(cancellationToken);
        return View(list);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var e = await _events.GetByIdAsync(id, cancellationToken);
        if (e is null)
            return NotFound();
        return View(e);
    }

    [Authorize(Roles = "Organizer,Admin")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewBag.Rooms = await _rooms.GetRoomsWithAvailabilityAsync(cancellationToken);
        var start = DateTime.Now.AddHours(1);
        return View(new CreateUpdateEventRequest
        {
            StartTime = start,
            EndTime = start.AddHours(2)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Organizer,Admin")]
    public async Task<IActionResult> Create(CreateUpdateEventRequest model, IFormFile? poster, CancellationToken cancellationToken)
    {
        ViewBag.Rooms = await _rooms.GetRoomsWithAvailabilityAsync(cancellationToken);

        var result = await _events.CreateAsync(model, CurrentUserId, cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        var id = result.Value;
        if (poster is { Length: > 0 })
        {
            await using var stream = poster.OpenReadStream();
            var saved = await _documents.SaveEventPosterAsync(stream, poster.FileName, cancellationToken);
            if (saved.Success)
                await _events.SetPosterFileNameAsync(id, saved.Value, CurrentUserId, IsAdmin, cancellationToken);
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Organizer,Admin")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var e = await _events.GetByIdAsync(id, cancellationToken);
        if (e is null)
            return NotFound();

        if (!IsAdmin && e.OrganizerId != CurrentUserId)
            return Forbid();

        if (!IsAdmin && e.Status != EventStatus.Draft)
            return Forbid();

        ViewBag.Rooms = await _rooms.GetRoomsWithAvailabilityAsync(cancellationToken);
        ViewBag.IsAdmin = IsAdmin;
        ViewBag.EventId = id;
        return View(new CreateUpdateEventRequest
        {
            Title = e.Title,
            StartTime = e.StartTime,
            EndTime = e.EndTime,
            Status = e.Status,
            RoomId = e.RoomId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Organizer,Admin")]
    public async Task<IActionResult> Edit(Guid id, CreateUpdateEventRequest model, IFormFile? poster, CancellationToken cancellationToken)
    {
        ViewBag.Rooms = await _rooms.GetRoomsWithAvailabilityAsync(cancellationToken);
        ViewBag.IsAdmin = IsAdmin;
        ViewBag.EventId = id;

        var r = await _events.UpdateAsync(id, model, CurrentUserId, IsAdmin, cancellationToken);
        if (!r.Success)
        {
            ModelState.AddModelError(string.Empty, r.Error!);
            return View(model);
        }

        if (poster is { Length: > 0 })
        {
            await using var stream = poster.OpenReadStream();
            var saved = await _documents.SaveEventPosterAsync(stream, poster.FileName, cancellationToken);
            if (saved.Success)
                await _events.SetPosterFileNameAsync(id, saved.Value, CurrentUserId, IsAdmin, cancellationToken);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Organizer,Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var r = await _events.DeleteAsync(id, CurrentUserId, IsAdmin, cancellationToken);
        if (!r.Success)
            TempData["Error"] = r.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Organizer,Admin")]
    public async Task<IActionResult> SubmitForApproval(Guid id, CancellationToken cancellationToken)
    {
        var r = await _events.SubmitForApprovalAsync(id, CurrentUserId, cancellationToken);
        if (!r.Success)
            TempData["Error"] = r.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        var r = await _events.ApproveAsync(id, CurrentUserId, cancellationToken);
        if (!r.Success)
            TempData["Error"] = r.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reject(Guid id, CancellationToken cancellationToken)
    {
        var r = await _events.RejectAsync(id, CurrentUserId, cancellationToken);
        if (!r.Success)
            TempData["Error"] = r.Error;
        return RedirectToAction(nameof(Index));
    }
}
