using EventMaster.Domain.Common;

namespace EventMaster.Domain.Entities;

public class Ticket : BaseEntity
{
    public decimal Price { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }

    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
