using EventMaster.Application.Common;
using EventMaster.Application.DTOs.Seats;

namespace EventMaster.Application.Services;

public interface ISeatService
{
    Task<IReadOnlyList<RoomSeatDto>> GetRoomSeatsAsync(Guid roomId, CancellationToken cancellationToken = default);
    Task<Result<int>> GenerateRoomSeatsAsync(GenerateRoomSeatsRequest request, Guid actingUserId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<Result> SetRoomSeatActiveAsync(Guid roomSeatId, bool isActive, Guid actingUserId, bool isAdmin, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<EventSeatDto>>> GetEventSeatMapAsync(Guid eventId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result> HoldSeatAsync(Guid eventId, Guid roomSeatId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result> ReleaseSeatAsync(Guid eventId, Guid roomSeatId, Guid currentUserId, CancellationToken cancellationToken = default);
}

