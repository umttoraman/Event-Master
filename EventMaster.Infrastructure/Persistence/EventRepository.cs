using EventMaster.Application.Abstractions;
using EventMaster.Domain.Entities;
using EventMaster.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventMaster.Infrastructure.Persistence;

public class EventRepository : Repository<Event>, IEventRepository
{
    public EventRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<bool> HasRoomTimeConflictAsync(
        Guid roomId,
        DateTime start,
        DateTime end,
        Guid? excludeEventId,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Events
            .Where(e => e.RoomId == roomId)
            .Where(e => e.Status == EventStatus.Pending || e.Status == EventStatus.Approved)
            .Where(e => e.StartTime < end && e.EndTime > start);

        if (excludeEventId.HasValue)
            query = query.Where(e => e.Id != excludeEventId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Event>> GetWithDetailsAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Events
            .AsNoTracking()
            .Include(e => e.Organizer)
            .Include(e => e.Room)
            .Include(e => e.Tickets)
            .OrderByDescending(e => e.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<Event?> GetWithDetailsByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Context.Events
            .AsNoTracking()
            .Include(e => e.Organizer)
            .Include(e => e.Room)
            .Include(e => e.Tickets)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }
}
