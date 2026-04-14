using EventMaster.Domain.Entities;
using EventMaster.Domain.Enums;

namespace EventMaster.Application.Abstractions;

public interface IEventRepository : IRepository<Event>
{
    /// <summary>
    /// True if another blocking event uses the same room and overlaps in time.
    /// </summary>
    Task<bool> HasRoomTimeConflictAsync(
        Guid roomId,
        DateTime start,
        DateTime end,
        Guid? excludeEventId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Event>> GetWithDetailsAsync(CancellationToken cancellationToken = default);
    Task<Event?> GetWithDetailsByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
