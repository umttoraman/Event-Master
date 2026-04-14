namespace EventMaster.Application.DTOs.Tickets;

public class PurchaseTicketRequest
{
    public Guid EventId { get; set; }
    public Guid RoomSeatId { get; set; }
    public decimal Price { get; set; }
}
