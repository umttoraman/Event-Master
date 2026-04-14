using EventMaster.Domain.Common;

namespace EventMaster.Domain.Entities;

/// <summary>
/// Activity / audit trail (event status changes, ticket purchases, etc.).
/// </summary>
public class AuditLog : BaseEntity
{
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string Details { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
