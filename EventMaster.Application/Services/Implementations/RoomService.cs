using EventMaster.Application.Abstractions;
using EventMaster.Application.DTOs.Rooms;
using EventMaster.Domain.Enums;

namespace EventMaster.Application.Services.Implementations;

public class RoomService : IRoomService
{
    private readonly IUnitOfWork _u;

    public RoomService(IUnitOfWork unitOfWork)
    {
        _u = unitOfWork;
    }

    public async Task<IReadOnlyList<RoomDto>> GetRoomsWithAvailabilityAsync(CancellationToken cancellationToken = default)
    {
        var rooms = await _u.Rooms.GetAllAsync(cancellationToken);
        var events = await _u.Events.GetWithDetailsAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var list = new List<RoomDto>();
        foreach (var r in rooms)
        {
            var upcoming = events.Count(e =>
                e.RoomId == r.Id &&
                e.Status is EventStatus.Pending or EventStatus.Approved &&
                e.EndTime >= now);

            list.Add(new RoomDto
            {
                Id = r.Id,
                RoomName = r.RoomName,
                Capacity = r.Capacity,
                IsAvailable = r.IsAvailable,
                UpcomingEventsCount = upcoming
            });
        }

        return list;
    }
}
