using EventMaster.Domain.Enums;

namespace EventMaster.Application.DTOs.Events;

public class CreateUpdateEventRequest
{
    public string Title { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public EventStatus Status { get; set; } = EventStatus.Draft;
    public Guid RoomId { get; set; }
}
