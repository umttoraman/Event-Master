using EventMaster.Domain.Enums;

namespace EventMaster.Application.Abstractions;

public interface ICurrentUserContext
{
    Guid? UserId { get; }
    string? UserName { get; }
    UserRole? Role { get; }
    bool IsAuthenticated { get; }
}
