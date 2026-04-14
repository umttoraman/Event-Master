using EventMaster.Application.Common;
using EventMaster.Application.DTOs.Tickets;

namespace EventMaster.Application.Services;

public interface ITicketService
{
    Task<TicketDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<Guid>> PurchaseAsync(PurchaseTicketRequest request, Guid buyerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TicketDto>> GetFinancialReportAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetTotalRevenueAsync(CancellationToken cancellationToken = default);
}
