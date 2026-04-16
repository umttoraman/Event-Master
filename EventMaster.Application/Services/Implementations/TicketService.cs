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

    public async Task<Result<PurchaseTicketsResultDto>> PurchaseAsync(PurchaseTicketRequest request, Guid buyerId, CancellationToken cancellationToken = default)
    {
        var ev = await _u.Events.GetWithDetailsByIdAsync(request.EventId, cancellationToken);
        if (ev is null)
            return Result<PurchaseTicketsResultDto>.Fail("Etkinlik bulunamadı.");

        if (ev.Status != EventStatus.Approved)
            return Result<PurchaseTicketsResultDto>.Fail("Sadece onaylanmış etkinliklere bilet alınabilir.");

        var room = ev.Room ?? await _u.Rooms.GetByIdAsync(ev.RoomId, cancellationToken);
        if (room is null)
            return Result<PurchaseTicketsResultDto>.Fail("Oda bulunamadı.");

        var seatIds = (request.RoomSeatIds ?? new List<Guid>())
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();
        if (seatIds.Count == 0)
            return Result<PurchaseTicketsResultDto>.Fail("En az 1 koltuk seçiniz.");

        // Capacity check
        var soldAlready = await _u.Tickets.CountAsync(t => t.EventId == ev.Id, cancellationToken);
        if (soldAlready + seatIds.Count > room.Capacity)
            return Result<PurchaseTicketsResultDto>.Fail("Oda kapasitesi aşılıyor.");

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

        // Load and validate seats
        var seats = new List<RoomSeat>();
        foreach (var id in seatIds)
        {
            var s = await _u.RoomSeats.GetByIdAsync(id, cancellationToken);
            if (s is null || s.RoomId != ev.RoomId || !s.IsActive)
                return Result<PurchaseTicketsResultDto>.Fail("Koltuk geçersiz.");
            seats.Add(s);
        }

        // Ensure none of selected seats are held by others or already sold
        foreach (var seat in seats)
        {
            var activeHold = (await _u.EventSeatHolds.FindAsync(
                    x => x.EventId == ev.Id && x.RoomSeatId == seat.Id && x.Status == SeatHoldStatus.Active,
                    cancellationToken))
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefault();
            if (activeHold is not null && activeHold.UserId != buyerId)
                return Result<PurchaseTicketsResultDto>.Fail($"Koltuk tutulmuş: {seat.Label}");

            var seatTaken = await _u.Tickets.AnyAsync(
                t => t.EventId == ev.Id && t.SeatNumber == seat.Label,
                cancellationToken);
            if (seatTaken)
                return Result<PurchaseTicketsResultDto>.Fail($"Koltuk dolu: {seat.Label}");
        }

        var ticketIds = new List<Guid>(seats.Count);
        foreach (var seat in seats)
        {
            var ticketId = Guid.NewGuid();
            var ticket = new Ticket
            {
                Id = ticketId,
                EventId = ev.Id,
                UserId = buyerId,
                SeatNumber = seat.Label,
                // Price is server-controlled; never trust client input.
                Price = TicketPrice,
                PurchaseDate = DateTime.UtcNow
            };
            await _u.Tickets.AddAsync(ticket, cancellationToken);
            ticketIds.Add(ticketId);

            // Mark hold as Purchased (if present) for history.
            var myHold = (await _u.EventSeatHolds.FindAsync(
                    x => x.EventId == ev.Id && x.RoomSeatId == seat.Id && x.UserId == buyerId && x.Status == SeatHoldStatus.Active,
                    cancellationToken))
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefault();
            if (myHold is not null)
            {
                myHold.Status = SeatHoldStatus.Purchased;
                await _u.EventSeatHolds.UpdateAsync(myHold, cancellationToken);
            }
        }

        await _u.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = buyerId,
            Action = "Ticket.Purchased",
            EntityType = nameof(Ticket),
            EntityId = ev.Id.ToString(),
            Details = $"Event={ev.Title}, Seats={string.Join(",", seats.Select(x => x.Label))}, Count={seats.Count}, Total={seats.Count * TicketPrice}",
            CreatedAtUtc = DateTime.UtcNow
        }, cancellationToken);

        await _notify.SendInAppAsync(buyerId, $"Bilet satın alındı: {ev.Title} / {seats.Count} koltuk", cancellationToken);

        await _u.SaveChangesAsync(cancellationToken);
        return Result<PurchaseTicketsResultDto>.Ok(new PurchaseTicketsResultDto
        {
            TicketIds = ticketIds,
            TotalPrice = ticketIds.Count * TicketPrice
        });
    }

    public async Task<IReadOnlyList<TicketDto>> GetUserTicketsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tickets = await _u.Tickets.FindAsync(t => t.UserId == userId, cancellationToken);
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
