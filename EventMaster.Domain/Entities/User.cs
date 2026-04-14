using EventMaster.Domain.Common;
using EventMaster.Domain.Enums;

namespace EventMaster.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public DateTime? LastLogin { get; set; }

    public ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();
    public ICollection<Ticket> PurchasedTickets { get; set; } = new List<Ticket>();
}
