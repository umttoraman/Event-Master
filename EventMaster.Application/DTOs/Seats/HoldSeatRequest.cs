namespace EventMaster.Application.DTOs.Seats;

public class HoldSeatRequest
{
    public Guid EventId { get; set; }
    public Guid RoomSeatId { get; set; }
}

