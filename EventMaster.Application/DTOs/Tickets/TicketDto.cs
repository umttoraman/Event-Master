namespace EventMaster.Application.DTOs.Tickets;

public class TicketDto
{
    public Guid Id { get; set; }
    public decimal Price { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string BuyerName { get; set; } = string.Empty;
}
