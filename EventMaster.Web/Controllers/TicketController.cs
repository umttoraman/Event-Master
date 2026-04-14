using System.Security.Claims;
using EventMaster.Application.DTOs.Tickets;
using EventMaster.Application.Services;
using EventMaster.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventMaster.Web.Controllers;

[Authorize]
public class TicketController : Controller
{
    private readonly ITicketService _tickets;
    private readonly IEventService _events;
    private readonly IDocumentService _documents;

    public TicketController(ITicketService tickets, IEventService events, IDocumentService documents)
    {
        _tickets = tickets;
        _events = events;
        _documents = documents;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Purchase(CancellationToken cancellationToken)
    {
        var all = await _events.GetAllAsync(cancellationToken);
        var approved = all.Where(e => e.Status == EventStatus.Approved).ToList();
        ViewBag.ApprovedEvents = approved;
        var model = new PurchaseTicketRequest();
        if (approved.Count > 0)
            model.EventId = approved[0].Id;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Purchase(PurchaseTicketRequest model, CancellationToken cancellationToken)
    {
        var result = await _tickets.PurchaseAsync(model, CurrentUserId, cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            var all = await _events.GetAllAsync(cancellationToken);
            ViewBag.ApprovedEvents = all.Where(e => e.Status == EventStatus.Approved).ToList();
            return View(model);
        }

        TempData["Message"] = "Bilet satın alındı.";
        return RedirectToAction(nameof(Purchase));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Report(CancellationToken cancellationToken)
    {
        var rows = await _tickets.GetFinancialReportAsync(cancellationToken);
        var total = await _tickets.GetTotalRevenueAsync(cancellationToken);
        ViewBag.TotalRevenue = total;
        return View(rows);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadPdf(Guid id, CancellationToken cancellationToken)
    {
        var ticket = await _tickets.GetByIdAsync(id, cancellationToken);
        if (ticket is null)
            return NotFound();

        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && ticket.UserId != CurrentUserId)
            return Forbid();

        var pdf = await _documents.GenerateTicketPdfAsync(ticket, cancellationToken);
        if (!pdf.Success)
            return BadRequest(pdf.Error);

        return File(pdf.Value!, "application/pdf", $"bilet-{id:N}.pdf");
    }
}
