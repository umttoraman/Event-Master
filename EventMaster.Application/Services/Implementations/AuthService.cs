using EventMaster.Application.Abstractions;
using EventMaster.Application.Common;
using EventMaster.Application.DTOs.Auth;
using EventMaster.Domain.Entities;
using EventMaster.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace EventMaster.Application.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AuthService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AuthUserDto>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return Result<AuthUserDto>.Fail("Kullanıcı adı ve şifre zorunludur.");

        var existing = await _unitOfWork.Users.AnyAsync(u => u.Username == request.Username.Trim(), cancellationToken);
        if (existing)
            return Result<AuthUserDto>.Fail("Bu kullanıcı adı zaten kullanılıyor.");

        // Public registration: only Customer (Admin/Organizer via seed or future admin UI)
        var role = UserRole.Customer;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username.Trim(),
            Role = role,
            LastLogin = null
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AuthUserDto>.Ok(new AuthUserDto { Id = user.Id, Username = user.Username, Role = user.Role });
    }

    public async Task<Result<AuthUserDto>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = (await _unitOfWork.Users.FindAsync(u => u.Username == request.Username.Trim(), cancellationToken)).FirstOrDefault();
        if (user is null)
            return Result<AuthUserDto>.Fail("Geçersiz kullanıcı adı veya şifre.");

        var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verify == PasswordVerificationResult.Failed)
            return Result<AuthUserDto>.Fail("Geçersiz kullanıcı adı veya şifre.");

        user.LastLogin = DateTime.UtcNow;
        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AuthUserDto>.Ok(new AuthUserDto { Id = user.Id, Username = user.Username, Role = user.Role });
    }
}
