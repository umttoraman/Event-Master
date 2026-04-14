using EventMaster.Domain.Common;
using EventMaster.Domain.Enums;

namespace EventMaster.Domain.Entities;

public class EventSeatHold : BaseEntity
{
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;

    public Guid RoomSeatId { get; set; }
    public RoomSeat RoomSeat { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public SeatHoldStatus Status { get; set; } = SeatHoldStatus.Active;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ReleasedAtUtc { get; set; }
}

