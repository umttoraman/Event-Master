namespace EventMaster.Application.DTOs.Tickets;

public class PurchaseTicketsResultDto
{
    public IReadOnlyList<Guid> TicketIds { get; set; } = Array.Empty<Guid>();
    public int Count => TicketIds.Count;
    public decimal TotalPrice { get; set; }
}

