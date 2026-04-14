using EventMaster.Application.Abstractions;
using EventMaster.Application.Common;
using EventMaster.Application.DTOs.Tickets;
using EventMaster.Domain.Entities;
using EventMaster.Domain.Enums;

namespace EventMaster.Application.Services.Implementations;

public class TicketService : ITicketService
{
    private readonly IUnitOfWork _u;
    private readonly INotificationService _notify;
    private const decimal TicketPrice = 100m;

    public TicketService(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _u = unitOfWork;
        _notify = notificationService;
    }

    public async Task<TicketDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var t = await _u.Tickets.GetByIdAsync(id, cancellationToken);
        if (t is null)
            return null;

        var ev = await _u.Events.GetWithDetailsByIdAsync(t.EventId, cancellationToken);
        var u = await _u.Users.GetByIdAsync(t.UserId, cancellationToken);
        return new TicketDto
        {
            Id = t.Id,
            Price = t.Price,
            SeatNumber = t.SeatNumber,
            PurchaseDate = t.PurchaseDate,
            EventId = t.EventId,
            EventTitle = ev?.Title ?? "",
            UserId = t.UserId,
            BuyerName = u?.Username ?? ""
        };
    }

    public async Task<Result<Guid>> PurchaseAsync(PurchaseTicketRequest request, Guid buyerId, CancellationToken cancellationToken = default)
    {
        var ev = await _u.Events.GetWithDetailsByIdAsync(request.EventId, cancellationToken);
        if (ev is null)
            return Result<Guid>.Fail("Etkinlik bulunamadı.");

        if (ev.Status != EventStatus.Approved)
            return Result<Guid>.Fail("Sadece onaylanmış etkinliklere bilet alınabilir.");

        var room = ev.Room ?? await _u.Rooms.GetByIdAsync(ev.RoomId, cancellationToken);
        if (room is null)
            return Result<Guid>.Fail("Oda bulunamadı.");

        if (request.RoomSeatId == Guid.Empty)
            return Result<Guid>.Fail("Koltuk seçiniz.");

        // Validate seat belongs to the event room and is active.
        var seat = await _u.RoomSeats.GetByIdAsync(request.RoomSeatId, cancellationToken);
        if (seat is null || seat.RoomId != ev.RoomId || !seat.IsActive)
            return Result<Guid>.Fail("Koltuk geçersiz.");

        // Expire holds for this event (best-effort, keeps unique Active hold constraint workable).
        var now = DateTime.UtcNow;
        var expired = await _u.EventSeatHolds.FindAsync(
            x => x.EventId == ev.Id && x.Status == SeatHoldStatus.Active && x.ExpiresAtUtc <= now,
            cancellationToken);
        foreach (var h in expired)
        {
            h.Status = SeatHoldStatus.Expired;
            await _u.EventSeatHolds.UpdateAsync(h, cancellationToken);
        }
        if (expired.Count > 0)
            await _u.SaveChangesAsync(cancellationToken);

        // Seat cannot be held by someone else at purchase time.
        var activeHold = (await _u.EventSeatHolds.FindAsync(
                x => x.EventId == ev.Id && x.RoomSeatId == seat.Id && x.Status == SeatHoldStatus.Active,
                cancellationToken))
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefault();
        if (activeHold is not null && activeHold.UserId != buyerId)
            return Result<Guid>.Fail("Bu koltuk başka bir kullanıcı tarafından tutulmuş.");

        var seatTaken = await _u.Tickets.AnyAsync(
            t => t.EventId == ev.Id && t.SeatNumber == seat.Label,
            cancellationToken);
        if (seatTaken)
            return Result<Guid>.Fail("Bu koltuk dolu.");

        var sold = await _u.Tickets.CountAsync(t => t.EventId == ev.Id, cancellationToken);
        if (sold >= room.Capacity)
            return Result<Guid>.Fail("Oda kapasitesi doldu.");

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            EventId = ev.Id,
            UserId = buyerId,
            SeatNumber = seat.Label,
            // Price is server-controlled; never trust client input.
            Price = TicketPrice,
            PurchaseDate = DateTime.UtcNow
        };

        await _u.Tickets.AddAsync(ticket, cancellationToken);

        // Mark hold as Purchased (if present) for history.
        if (activeHold is not null && activeHold.UserId == buyerId)
        {
            activeHold.Status = SeatHoldStatus.Purchased;
            await _u.EventSeatHolds.UpdateAsync(activeHold, cancellationToken);
        }

        await _u.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = buyerId,
            Action = "Ticket.Purchased",
            EntityType = nameof(Ticket),
            EntityId = ticket.Id.ToString(),
            Details = $"Event={ev.Title}, Seat={ticket.SeatNumber}, Price={ticket.Price}",
            CreatedAtUtc = DateTime.UtcNow
        }, cancellationToken);

        var buyer = await _u.Users.GetByIdAsync(buyerId, cancellationToken);
        await _notify.SendInAppAsync(buyerId, $"Bilet satın alındı: {ev.Title} / Koltuk {ticket.SeatNumber}", cancellationToken);

        await _u.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Ok(ticket.Id);
    }

    public async Task<IReadOnlyList<TicketDto>> GetFinancialReportAsync(CancellationToken cancellationToken = default)
    {
        var tickets = await _u.Tickets.GetAllAsync(cancellationToken);
        var events = await _u.Events.GetWithDetailsAsync(cancellationToken);
        var users = await _u.Users.GetAllAsync(cancellationToken);

        return tickets.Select(t =>
        {
            var ev = events.FirstOrDefault(e => e.Id == t.EventId);
            var u = users.FirstOrDefault(x => x.Id == t.UserId);
            return new TicketDto
            {
                Id = t.Id,
                Price = t.Price,
                SeatNumber = t.SeatNumber,
                PurchaseDate = t.PurchaseDate,
                EventId = t.EventId,
                EventTitle = ev?.Title ?? "",
                UserId = t.UserId,
                BuyerName = u?.Username ?? ""
            };
        }).OrderByDescending(t => t.PurchaseDate).ToList();
    }

    public async Task<decimal> GetTotalRevenueAsync(CancellationToken cancellationToken = default)
    {
        var tickets = await _u.Tickets.GetAllAsync(cancellationToken);
        return tickets.Sum(t => t.Price);
    }
}
