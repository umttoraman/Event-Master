namespace EventMaster.Application.DTOs.Rooms;

public class RoomDto
{
    public Guid Id { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public bool IsAvailable { get; set; }
    public int UpcomingEventsCount { get; set; }
}
