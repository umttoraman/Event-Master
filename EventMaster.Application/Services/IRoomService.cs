using EventMaster.Application.DTOs.Rooms;

namespace EventMaster.Application.Services;

public interface IRoomService
{
    Task<IReadOnlyList<RoomDto>> GetRoomsWithAvailabilityAsync(CancellationToken cancellationToken = default);
}
