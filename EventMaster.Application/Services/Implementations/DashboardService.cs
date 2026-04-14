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

    public async Task<DashboardStatsDto> GetStatsAsync(Guid currentUserId, bool isAdmin, bool isOrganizer, CancellationToken cancellationToken = default)
    {
        var events = await _u.Events.GetAllAsync(cancellationToken);
        var rooms = await _u.Rooms.GetAllAsync(cancellationToken);
        var tickets = await _u.Tickets.GetAllAsync(cancellationToken);
        var visibleEvents = isAdmin
            ? events
            : isOrganizer
                ? events.Where(e => e.OrganizerId == currentUserId).ToList()
                : events.Where(e => e.Status == EventStatus.Approved).ToList();

        var eventIds = visibleEvents.Select(e => e.Id).ToHashSet();
        var visibleRevenue = isAdmin || isOrganizer
            ? tickets.Where(t => eventIds.Contains(t.EventId)).Sum(t => t.Price)
            : 0m;

        return new DashboardStatsDto
        {
            TotalEvents = visibleEvents.Count,
            ActiveRooms = rooms.Count(r => r.IsAvailable),
            TotalRevenue = visibleRevenue,
            PendingApprovals = isAdmin
                ? events.Count(e => e.Status == EventStatus.Pending)
                : 0
        };
    }
}
