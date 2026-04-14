using System.Security.Claims;
using EventMaster.Application.Abstractions;
using EventMaster.Domain.Enums;

namespace EventMaster.Web.Services;

public class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _http;

    public CurrentUserContext(IHttpContextAccessor http)
    {
        _http = http;
    }

    public Guid? UserId
    {
        get
        {
            var v = _http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(v, out var g) ? g : null;
        }
    }

    public string? UserName => _http.HttpContext?.User.FindFirstValue(ClaimTypes.Name);

    public UserRole? Role
    {
        get
        {
            var v = _http.HttpContext?.User.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<UserRole>(v, out var r) ? r : null;
        }
    }

    public bool IsAuthenticated => _http.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
