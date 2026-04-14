using EventMaster.Domain.Enums;

namespace EventMaster.Application.DTOs.Auth;

public class AuthUserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public UserRole Role { get; set; }
}
