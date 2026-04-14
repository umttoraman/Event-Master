using EventMaster.Domain.Enums;

namespace EventMaster.Application.DTOs.Events;

public class EventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public EventStatus Status { get; set; }
    public Guid OrganizerId { get; set; }
    public string OrganizerName { get; set; } = string.Empty;
    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string? PosterFileName { get; set; }
    public int TicketsSold { get; set; }
}
