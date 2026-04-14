namespace EventMaster.Domain.Common;

/// <summary>
/// Base type for all persisted entities (Code-First primary key).
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }
}
