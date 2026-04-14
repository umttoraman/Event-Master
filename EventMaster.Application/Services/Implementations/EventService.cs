using EventMaster.Application.Abstractions;
using EventMaster.Application.Common;
using EventMaster.Application.DTOs.Events;
using EventMaster.Domain.Entities;
using EventMaster.Domain.Enums;

namespace EventMaster.Application.Services.Implementations;

public class EventService : IEventService
{
    private readonly IUnitOfWork _u;
    private readonly INotificationService _notify;

    public EventService(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _u = unitOfWork;
        _notify = notificationService;
    }

    public async Task<IReadOnlyList<EventDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _u.Events.GetWithDetailsAsync(cancellationToken);
        return list.Select(Map).ToList();
    }

    public async Task<EventDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _u.Events.GetWithDetailsByIdAsync(id, cancellationToken);
        return e is null ? null : Map(e);
    }

    public async Task<Result<Guid>> CreateAsync(CreateUpdateEventRequest request, Guid organizerId, CancellationToken cancellationToken = default)
    {
        var startUtc = NormalizeToUtc(request.StartTime);
        var endUtc = NormalizeToUtc(request.EndTime);

        if (endUtc <= startUtc)
            return Result<Guid>.Fail("Bitiş saati başlangıçtan sonra olmalıdır.");

        var room = await _u.Rooms.GetByIdAsync(request.RoomId, cancellationToken);
        if (room is null)
            return Result<Guid>.Fail("Oda bulunamadı.");

        var entity = new Event
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            StartTime = startUtc,
            EndTime = endUtc,
            Status = EventStatus.Draft,
            OrganizerId = organizerId,
            RoomId = request.RoomId
        };

        await _u.Events.AddAsync(entity, cancellationToken);
        await AddAuditAsync("Event.Created", nameof(Event), entity.Id.ToString(), $"Title={entity.Title}", organizerId, cancellationToken);
        await _u.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Ok(entity.Id);
    }

    public async Task<Result> UpdateAsync(Guid id, CreateUpdateEventRequest request, Guid actingUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var e = await _u.Events.GetByIdAsync(id, cancellationToken);
        if (e is null)
            return Result.Fail("Etkinlik bulunamadı.");

        if (!isAdmin && e.OrganizerId != actingUserId)
            return Result.Fail("Bu etkinliği düzenleme yetkiniz yok.");

        if (!isAdmin && e.Status != EventStatus.Draft)
            return Result.Fail("Sadece taslak etkinlikler düzenlenebilir.");

        var startUtc = NormalizeToUtc(request.StartTime);
        var endUtc = NormalizeToUtc(request.EndTime);

        if (endUtc <= startUtc)
            return Result.Fail("Bitiş saati başlangıçtan sonra olmalıdır.");

        var room = await _u.Rooms.GetByIdAsync(request.RoomId, cancellationToken);
        if (room is null)
            return Result.Fail("Oda bulunamadı.");

        e.Title = request.Title.Trim();
        e.StartTime = startUtc;
        e.EndTime = endUtc;
        e.RoomId = request.RoomId;
        if (isAdmin)
            e.Status = request.Status;

        await _u.Events.UpdateAsync(e, cancellationToken);
        await AddAuditAsync("Event.Updated", nameof(Event), e.Id.ToString(), $"Title={e.Title}", actingUserId, cancellationToken);
        await _u.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(Guid id, Guid actingUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var e = await _u.Events.GetByIdAsync(id, cancellationToken);
        if (e is null)
            return Result.Fail("Etkinlik bulunamadı.");

        if (!isAdmin && e.OrganizerId != actingUserId)
            return Result.Fail("Silme yetkiniz yok.");

        if (!isAdmin && e.Status != EventStatus.Draft)
            return Result.Fail("Sadece taslak etkinlik silinebilir.");

        await _u.Events.DeleteAsync(e, cancellationToken);
        await AddAuditAsync("Event.Deleted", nameof(Event), id.ToString(), "", actingUserId, cancellationToken);
        await _u.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> SubmitForApprovalAsync(Guid id, Guid organizerId, CancellationToken cancellationToken = default)
    {
        var e = await _u.Events.GetByIdAsync(id, cancellationToken);
        if (e is null)
            return Result.Fail("Etkinlik bulunamadı.");

        if (e.OrganizerId != organizerId)
            return Result.Fail("Bu etkinlik size ait değil.");

        if (e.Status != EventStatus.Draft)
            return Result.Fail("Sadece taslaklar onaya gönderilebilir.");

        var conflict = await _u.Events.HasRoomTimeConflictAsync(e.RoomId, e.StartTime, e.EndTime, e.Id, cancellationToken);
        if (conflict)
            return Result.Fail("Bu zaman aralığında aynı oda için başka onaylı veya bekleyen bir etkinlik var.");

        var old = e.Status;
        e.Status = EventStatus.Pending;
        await _u.Events.UpdateAsync(e, cancellationToken);
        await AddAuditAsync("Event.StatusChanged", nameof(Event), e.Id.ToString(), $"{old}->{e.Status}", organizerId, cancellationToken);
        await _notify.SendInAppAsync(organizerId, $"Etkinlik onaya gönderildi: {e.Title}", cancellationToken);
        await _u.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> ApproveAsync(Guid id, Guid adminId, CancellationToken cancellationToken = default)
    {
        var e = await _u.Events.GetByIdAsync(id, cancellationToken);
        if (e is null)
            return Result.Fail("Etkinlik bulunamadı.");

        if (e.Status != EventStatus.Pending)
            return Result.Fail("Sadece bekleyen etkinlikler onaylanabilir.");

        var conflict = await _u.Events.HasRoomTimeConflictAsync(e.RoomId, e.StartTime, e.EndTime, e.Id, cancellationToken);
        if (conflict)
            return Result.Fail("Çakışma: aynı odada bu zaman diliminde başka bir etkinlik var.");

        var old = e.Status;
        e.Status = EventStatus.Approved;
        await _u.Events.UpdateAsync(e, cancellationToken);
        await AddAuditAsync("Event.StatusChanged", nameof(Event), e.Id.ToString(), $"{old}->{e.Status}", adminId, cancellationToken);

        var org = await _u.Users.GetByIdAsync(e.OrganizerId, cancellationToken);
        if (org is not null)
            await _notify.SendInAppAsync(e.OrganizerId, $"Etkinlik onaylandı: {e.Title}", cancellationToken);

        await _u.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> SetPosterFileNameAsync(Guid id, string? posterFileName, Guid actingUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var e = await _u.Events.GetByIdAsync(id, cancellationToken);
        if (e is null)
            return Result.Fail("Etkinlik bulunamadı.");

        if (!isAdmin && e.OrganizerId != actingUserId)
            return Result.Fail("Poster güncelleme yetkiniz yok.");

        e.PosterFileName = string.IsNullOrWhiteSpace(posterFileName) ? null : posterFileName.Trim();
        await _u.Events.UpdateAsync(e, cancellationToken);
        await AddAuditAsync("Event.PosterUpdated", nameof(Event), e.Id.ToString(), e.PosterFileName ?? "", actingUserId, cancellationToken);
        await _u.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> RejectAsync(Guid id, Guid adminId, CancellationToken cancellationToken = default)
    {
        var e = await _u.Events.GetByIdAsync(id, cancellationToken);
        if (e is null)
            return Result.Fail("Etkinlik bulunamadı.");

        if (e.Status != EventStatus.Pending)
            return Result.Fail("Sadece bekleyen etkinlikler reddedilebilir.");

        var old = e.Status;
        e.Status = EventStatus.Draft;
        await _u.Events.UpdateAsync(e, cancellationToken);
        await AddAuditAsync("Event.StatusChanged", nameof(Event), e.Id.ToString(), $"{old}->{e.Status} (reject)", adminId, cancellationToken);
        await _notify.SendInAppAsync(e.OrganizerId, $"Etkinlik reddedildi (taslağa alındı): {e.Title}", cancellationToken);
        await _u.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    private static EventDto Map(Event e)
    {
        return new EventDto
        {
            Id = e.Id,
            Title = e.Title,
            StartTime = e.StartTime,
            EndTime = e.EndTime,
            Status = e.Status,
            OrganizerId = e.OrganizerId,
            OrganizerName = e.Organizer?.Username ?? "",
            RoomId = e.RoomId,
            RoomName = e.Room?.RoomName ?? "",
            PosterFileName = e.PosterFileName,
            TicketsSold = e.Tickets?.Count ?? 0
        };
    }

    private async Task AddAuditAsync(string action, string entityType, string? entityId, string details, Guid? userId, CancellationToken cancellationToken)
    {
        await _u.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            CreatedAtUtc = DateTime.UtcNow
        }, cancellationToken);
    }

    private static DateTime NormalizeToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime()
        };
    }
}
