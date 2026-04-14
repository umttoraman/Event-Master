using EventMaster.Application.Common;
using EventMaster.Application.DTOs.Tickets;

namespace EventMaster.Application.Services;

public interface IDocumentService
{
    Task<Result<string>> SaveEventPosterAsync(Stream content, string originalFileName, CancellationToken cancellationToken = default);
    Task<Result<byte[]>> GenerateTicketPdfAsync(TicketDto ticket, CancellationToken cancellationToken = default);
}
