namespace EventMaster.Application.DTOs.Tickets;

public class PurchaseTicketRequest
{
    public Guid EventId { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
