using EventMaster.Application.Abstractions;
using EventMaster.Application.Common;
using EventMaster.Application.DTOs.Seats;
using EventMaster.Domain.Entities;
using EventMaster.Domain.Enums;

namespace EventMaster.Application.Services.Implementations;

public class SeatService : ISeatService
{
    private readonly IUnitOfWork _u;
    private static readonly TimeSpan HoldDuration = TimeSpan.FromMinutes(10);

    public SeatService(IUnitOfWork unitOfWork)
    {
        _u = unitOfWork;
    }

    public async Task<IReadOnlyList<RoomSeatDto>> GetRoomSeatsAsync(Guid roomId, CancellationToken cancellationToken = default)
    {
        var seats = await _u.RoomSeats.FindAsync(x => x.RoomId == roomId, cancellationToken);
        return seats
            .OrderBy(x => x.Section)
            .ThenBy(x => x.Row)
            .ThenBy(x => x.Number)
            .ThenBy(x => x.Label)
            .Select(Map)
            .ToList();
    }

    public async Task<Result<int>> GenerateRoomSeatsAsync(
        GenerateRoomSeatsRequest request,
        Guid actingUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        if (!isAdmin)
            return Result<int>.Fail("Sadece admin koltuk düzeni oluşturabilir.");

        if (request.Rows <= 0 || request.SeatsPerRow <= 0)
            return Result<int>.Fail("Satır ve koltuk sayısı 0'dan büyük olmalıdır.");

        var room = await _u.Rooms.GetByIdAsync(request.RoomId, cancellationToken);
        if (room is null)
            return Result<int>.Fail("Oda bulunamadı.");

        if (request.ReplaceExisting)
        {
            var existing = await _u.RoomSeats.FindAsync(x => x.RoomId == request.RoomId, cancellationToken);
            foreach (var s in existing)
                await _u.RoomSeats.DeleteAsync(s, cancellationToken);
        }

        var created = 0;
        for (var r = 1; r <= request.Rows; r++)
        {
            var rowLabel = ToExcelLetters(r);
            for (var n = 1; n <= request.SeatsPerRow; n++)
            {
                var label = $"{rowLabel}{n}";
                var seat = new RoomSeat
                {
                    Id = Guid.NewGuid(),
                    RoomId = request.RoomId,
                    Label = label,
                    Section = string.IsNullOrWhiteSpace(request.Section) ? null : request.Section.Trim(),
                    Row = rowLabel,
                    Number = n,
                    IsActive = true
                };
                await _u.RoomSeats.AddAsync(seat, cancellationToken);
                created++;
            }
        }

        await _u.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actingUserId,
            Action = "RoomSeats.Generated",
            EntityType = nameof(Room),
            EntityId = request.RoomId.ToString(),
            Details = $"Rows={request.Rows}, SeatsPerRow={request.SeatsPerRow}, Section={request.Section ?? ""}, Replace={request.ReplaceExisting}",
            CreatedAtUtc = DateTime.UtcNow
        }, cancellationToken);

        await _u.SaveChangesAsync(cancellationToken);
        return Result<int>.Ok(created);
    }

    public async Task<Result> SetRoomSeatActiveAsync(
        Guid roomSeatId,
        bool isActive,
        Guid actingUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        if (!isAdmin)
            return Result.Fail("Sadece admin koltuk durumunu değiştirebilir.");

        var seat = await _u.RoomSeats.GetByIdAsync(roomSeatId, cancellationToken);
        if (seat is null)
            return Result.Fail("Koltuk bulunamadı.");

        seat.IsActive = isActive;
        await _u.RoomSeats.UpdateAsync(seat, cancellationToken);

        await _u.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actingUserId,
            Action = "RoomSeat.Updated",
            EntityType = nameof(RoomSeat),
            EntityId = roomSeatId.ToString(),
            Details = $"IsActive={isActive}",
            CreatedAtUtc = DateTime.UtcNow
        }, cancellationToken);

        await _u.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result<IReadOnlyList<EventSeatDto>>> GetEventSeatMapAsync(
        Guid eventId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var ev = await _u.Events.GetWithDetailsByIdAsync(eventId, cancellationToken);
        if (ev is null)
            return Result<IReadOnlyList<EventSeatDto>>.Fail("Etkinlik bulunamadı.");

        if (ev.Status != EventStatus.Approved)
            return Result<IReadOnlyList<EventSeatDto>>.Fail("Sadece onaylı etkinlikler için koltuk seçilebilir.");

        await ExpireHoldsAsync(eventId, cancellationToken);

        var roomSeats = await _u.RoomSeats.FindAsync(x => x.RoomId == ev.RoomId && x.IsActive, cancellationToken);
        if (roomSeats.Count == 0)
            return Result<IReadOnlyList<EventSeatDto>>.Fail("Bu oda için koltuk düzeni tanımlanmamış.");

        var tickets = await _u.Tickets.FindAsync(x => x.EventId == eventId, cancellationToken);
        var soldLabels = tickets.Select(x => x.SeatNumber).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var holds = await _u.EventSeatHolds.FindAsync(
            x => x.EventId == eventId && x.Status == SeatHoldStatus.Active,
            cancellationToken);
        var holdBySeatId = holds
            .GroupBy(x => x.RoomSeatId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedAtUtc).First());

        var list = new List<EventSeatDto>(roomSeats.Count);
        foreach (var s in roomSeats)
        {
            var status = "available";

            if (soldLabels.Contains(s.Label))
            {
                status = "sold";
            }
            else if (holdBySeatId.TryGetValue(s.Id, out var hold))
            {
                status = hold.UserId == currentUserId ? "heldByMe" : "heldByOther";
            }

            list.Add(new EventSeatDto
            {
                RoomSeatId = s.Id,
                Label = s.Label,
                Section = s.Section,
                Row = s.Row,
                Number = s.Number,
                Status = status
            });
        }

        var ordered = list
            .OrderBy(x => x.Section)
            .ThenBy(x => x.Row)
            .ThenBy(x => x.Number)
            .ThenBy(x => x.Label)
            .ToList();

        return Result<IReadOnlyList<EventSeatDto>>.Ok(ordered);
    }

    public async Task<Result> HoldSeatAsync(Guid eventId, Guid roomSeatId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var ev = await _u.Events.GetWithDetailsByIdAsync(eventId, cancellationToken);
        if (ev is null)
            return Result.Fail("Etkinlik bulunamadı.");

        if (ev.Status != EventStatus.Approved)
            return Result.Fail("Sadece onaylı etkinlikler için koltuk tutulabilir.");

        await ExpireHoldsAsync(eventId, cancellationToken);

        var seat = await _u.RoomSeats.GetByIdAsync(roomSeatId, cancellationToken);
        if (seat is null || seat.RoomId != ev.RoomId || !seat.IsActive)
            return Result.Fail("Koltuk geçersiz.");

        var sold = await _u.Tickets.AnyAsync(x => x.EventId == eventId && x.SeatNumber == seat.Label, cancellationToken);
        if (sold)
            return Result.Fail("Bu koltuk satılmış.");

        var existingActive = await _u.EventSeatHolds.FindAsync(
            x => x.EventId == eventId && x.RoomSeatId == roomSeatId && x.Status == SeatHoldStatus.Active,
            cancellationToken);
        var existing = existingActive.OrderByDescending(x => x.CreatedAtUtc).FirstOrDefault();

        if (existing is not null && existing.UserId != currentUserId)
            return Result.Fail("Bu koltuk başka bir kullanıcı tarafından tutulmuş.");

        var now = DateTime.UtcNow;
        if (existing is not null)
        {
            existing.ExpiresAtUtc = now.Add(HoldDuration);
            await _u.EventSeatHolds.UpdateAsync(existing, cancellationToken);
        }
        else
        {
            await _u.EventSeatHolds.AddAsync(new EventSeatHold
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                RoomSeatId = roomSeatId,
                UserId = currentUserId,
                Status = SeatHoldStatus.Active,
                CreatedAtUtc = now,
                ExpiresAtUtc = now.Add(HoldDuration)
            }, cancellationToken);
        }

        await _u.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> ReleaseSeatAsync(Guid eventId, Guid roomSeatId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        await ExpireHoldsAsync(eventId, cancellationToken);

        var holds = await _u.EventSeatHolds.FindAsync(
            x => x.EventId == eventId && x.RoomSeatId == roomSeatId && x.UserId == currentUserId && x.Status == SeatHoldStatus.Active,
            cancellationToken);
        var hold = holds.OrderByDescending(x => x.CreatedAtUtc).FirstOrDefault();
        if (hold is null)
            return Result.Ok(); // idempotent

        hold.Status = SeatHoldStatus.Released;
        hold.ReleasedAtUtc = DateTime.UtcNow;
        await _u.EventSeatHolds.UpdateAsync(hold, cancellationToken);
        await _u.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    private async Task ExpireHoldsAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var expired = await _u.EventSeatHolds.FindAsync(
            x => x.EventId == eventId && x.Status == SeatHoldStatus.Active && x.ExpiresAtUtc <= now,
            cancellationToken);

        if (expired.Count == 0)
            return;

        foreach (var h in expired)
            h.Status = SeatHoldStatus.Expired;

        foreach (var h in expired)
            await _u.EventSeatHolds.UpdateAsync(h, cancellationToken);

        await _u.SaveChangesAsync(cancellationToken);
    }

    private static RoomSeatDto Map(RoomSeat s) => new()
    {
        Id = s.Id,
        RoomId = s.RoomId,
        Label = s.Label,
        Section = s.Section,
        Row = s.Row,
        Number = s.Number,
        IsActive = s.IsActive
    };

    private static string ToExcelLetters(int number)
    {
        // 1 -> A, 26 -> Z, 27 -> AA
        var n = number;
        var chars = new Stack<char>();
        while (n > 0)
        {
            n--; // 0-based
            chars.Push((char)('A' + (n % 26)));
            n /= 26;
        }
        return new string(chars.ToArray());
    }
}

