namespace EventMaster.Application.DTOs.Seats;

public class GenerateRoomSeatsRequest
{
    public Guid RoomId { get; set; }
    public int Rows { get; set; } = 10;
    public int SeatsPerRow { get; set; } = 10;
    public string? Section { get; set; }

    /// <summary>
    /// If true, deletes existing seats for the room and recreates them.
    /// </summary>
    public bool ReplaceExisting { get; set; } = true;
}

