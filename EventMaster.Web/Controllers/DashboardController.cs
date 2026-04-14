using EventMaster.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventMaster.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboard;
    private readonly IAuditService _audit;

    public DashboardController(IDashboardService dashboard, IAuditService audit)
    {
        _dashboard = dashboard;
        _audit = audit;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var stats = await _dashboard.GetStatsAsync(cancellationToken);
        return View(stats);
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AuditLogs(CancellationToken cancellationToken)
    {
        var logs = await _audit.GetRecentAsync(200, cancellationToken);
        return View(logs);
    }
}
