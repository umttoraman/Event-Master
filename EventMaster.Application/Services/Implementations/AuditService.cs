using EventMaster.Application.Abstractions;
using EventMaster.Application.DTOs.Audit;

namespace EventMaster.Application.Services.Implementations;

public class AuditService : IAuditService
{
    private readonly IUnitOfWork _u;

    public AuditService(IUnitOfWork unitOfWork)
    {
        _u = unitOfWork;
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetRecentAsync(int take = 100, CancellationToken cancellationToken = default)
    {
        var logs = await _u.AuditLogs.GetAllAsync(cancellationToken);
        var users = await _u.Users.GetAllAsync(cancellationToken);

        return logs
            .OrderByDescending(l => l.CreatedAtUtc)
            .Take(take)
            .Select(l =>
            {
                var u = l.UserId is { } uid ? users.FirstOrDefault(x => x.Id == uid) : null;
                return new AuditLogDto
                {
                    Id = l.Id,
                    UserName = u?.Username,
                    Action = l.Action,
                    EntityType = l.EntityType,
                    EntityId = l.EntityId,
                    Details = l.Details,
                    CreatedAtUtc = l.CreatedAtUtc
                };
            })
            .ToList();
    }
}
