using EventMaster.Application.DTOs.Dashboard;

namespace EventMaster.Application.Services;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync(Guid currentUserId, bool isAdmin, bool isOrganizer, CancellationToken cancellationToken = default);
}
