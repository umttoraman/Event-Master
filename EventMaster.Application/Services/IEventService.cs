using EventMaster.Application.Common;
using EventMaster.Application.DTOs.Events;

namespace EventMaster.Application.Services;

public interface IEventService
{
    Task<IReadOnlyList<EventDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<EventDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<Guid>> CreateAsync(CreateUpdateEventRequest request, Guid organizerId, CancellationToken cancellationToken = default);
    Task<Result> UpdateAsync(Guid id, CreateUpdateEventRequest request, Guid actingUserId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, Guid actingUserId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<Result> SubmitForApprovalAsync(Guid id, Guid organizerId, CancellationToken cancellationToken = default);
    Task<Result> ApproveAsync(Guid id, Guid adminId, CancellationToken cancellationToken = default);
    Task<Result> RejectAsync(Guid id, Guid adminId, CancellationToken cancellationToken = default);
    Task<Result> SetPosterFileNameAsync(Guid id, string? posterFileName, Guid actingUserId, bool isAdmin, CancellationToken cancellationToken = default);
}
