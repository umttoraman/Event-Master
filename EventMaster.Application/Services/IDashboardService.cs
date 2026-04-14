using EventMaster.Application.DTOs.Dashboard;

namespace EventMaster.Application.Services;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);
}
