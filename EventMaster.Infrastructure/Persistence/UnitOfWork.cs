using EventMaster.Application.Abstractions;
using EventMaster.Domain.Entities;

namespace EventMaster.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly Lazy<IRepository<User>> _users;
    private readonly Lazy<IRepository<Room>> _rooms;
    private readonly Lazy<IEventRepository> _events;
    private readonly Lazy<IRepository<Ticket>> _tickets;
    private readonly Lazy<IRepository<AuditLog>> _auditLogs;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        _users = new Lazy<IRepository<User>>(() => new Repository<User>(context));
        _rooms = new Lazy<IRepository<Room>>(() => new Repository<Room>(context));
        _events = new Lazy<IEventRepository>(() => new EventRepository(context));
        _tickets = new Lazy<IRepository<Ticket>>(() => new Repository<Ticket>(context));
        _auditLogs = new Lazy<IRepository<AuditLog>>(() => new Repository<AuditLog>(context));
    }

    public IRepository<User> Users => _users.Value;
    public IRepository<Room> Rooms => _rooms.Value;
    public IEventRepository Events => _events.Value;
    public IRepository<Ticket> Tickets => _tickets.Value;
    public IRepository<AuditLog> AuditLogs => _auditLogs.Value;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public void Dispose() => _context.Dispose();
}
