using EventMaster.Domain.Common;

namespace EventMaster.Domain.Entities;

public class RoomSeat : BaseEntity
{
    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

    /// <summary>
    /// Human-friendly seat label shown to users (e.g., "A12", "B-07", "VIP-A1").
    /// Stored in <see cref="Ticket.SeatNumber"/> upon purchase.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    public string? Section { get; set; }
    public string? Row { get; set; }
    public int? Number { get; set; }

    public bool IsActive { get; set; } = true;
}

