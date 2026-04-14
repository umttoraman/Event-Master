using EventMaster.Domain.Common;

namespace EventMaster.Domain.Entities;

public class Room : BaseEntity
{
    public string RoomName { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public bool IsAvailable { get; set; }

    public ICollection<Event> Events { get; set; } = new List<Event>();
}
