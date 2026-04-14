using EventMaster.Application.Common;
using EventMaster.Application.DTOs.Auth;

namespace EventMaster.Application.Services;

public interface IAuthService
{
    Task<Result<AuthUserDto>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthUserDto>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
