namespace EventMaster.Application.DTOs.Seats;

public class EventSeatDto
{
    public Guid RoomSeatId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Section { get; set; }
    public string? Row { get; set; }
    public int? Number { get; set; }

    /// <summary>available | sold | heldByMe | heldByOther</summary>
    public string Status { get; set; } = "available";
}

