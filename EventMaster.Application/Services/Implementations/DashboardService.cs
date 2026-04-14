using EventMaster.Application.Abstractions;
using EventMaster.Application.DTOs.Dashboard;
using EventMaster.Domain.Enums;

namespace EventMaster.Application.Services.Implementations;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _u;

    public DashboardService(IUnitOfWork unitOfWork)
    {
        _u = unitOfWork;
    }

    public async Task<DashboardStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var events = await _u.Events.GetAllAsync(cancellationToken);
        var rooms = await _u.Rooms.GetAllAsync(cancellationToken);
        var tickets = await _u.Tickets.GetAllAsync(cancellationToken);

        return new DashboardStatsDto
        {
            TotalEvents = events.Count,
            ActiveRooms = rooms.Count(r => r.IsAvailable),
            TotalRevenue = tickets.Sum(t => t.Price),
            PendingApprovals = events.Count(e => e.Status == EventStatus.Pending)
        };
    }
}
