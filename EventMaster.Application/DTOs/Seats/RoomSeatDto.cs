namespace EventMaster.Application.DTOs.Seats;

public class RoomSeatDto
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Section { get; set; }
    public string? Row { get; set; }
    public int? Number { get; set; }
    public bool IsActive { get; set; }
}

