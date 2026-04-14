using EventMaster.Domain.Entities;

namespace EventMaster.Application.Abstractions;

public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<Room> Rooms { get; }
    IEventRepository Events { get; }
    IRepository<Ticket> Tickets { get; }
    IRepository<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
