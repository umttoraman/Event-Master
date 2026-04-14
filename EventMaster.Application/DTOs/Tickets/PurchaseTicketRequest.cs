namespace EventMaster.Application.DTOs.Tickets;

public class PurchaseTicketRequest
{
    public Guid EventId { get; set; }
    public List<Guid> RoomSeatIds { get; set; } = new();
    public decimal Price { get; set; }
}
