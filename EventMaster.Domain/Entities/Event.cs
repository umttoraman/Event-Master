using EventMaster.Domain.Common;
using EventMaster.Domain.Enums;

namespace EventMaster.Domain.Entities;

public class Event : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public EventStatus Status { get; set; }

    public Guid OrganizerId { get; set; }
    public User Organizer { get; set; } = null!;

    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

    /// <summary>Relative file name under wwwroot/uploads/posters (optional).</summary>
    public string? PosterFileName { get; set; }

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
