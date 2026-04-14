using EventMaster.Application.DTOs.Audit;

namespace EventMaster.Application.Services;

public interface IAuditService
{
    Task<IReadOnlyList<AuditLogDto>> GetRecentAsync(int take = 100, CancellationToken cancellationToken = default);
}
