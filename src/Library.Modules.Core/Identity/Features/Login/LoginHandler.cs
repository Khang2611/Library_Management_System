using FluentValidation;
using Library.Modules.Core.Identity.Data;
using Library.Modules.Core.Identity.Services;
using Library.Modules.Core.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Library.Modules.Core.Identity.Features.Login;

public sealed class LoginHandler : IRequestHandler<LoginRequest, ApiResponse<LoginResponse>>
{
    private readonly IdentityDbContext _db;
    private readonly IValidator<LoginRequest> _validator;
    private readonly JwtTokenService _jwt;

    public LoginHandler(IdentityDbContext db, IValidator<LoginRequest> validator, JwtTokenService jwt)
    {
        _db = db;
        _validator = validator;
        _jwt = jwt;
    }

    public async Task<ApiResponse<LoginResponse>> Handle(LoginRequest request, CancellationToken ct)
    {
        // â”€â”€ Validate â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return ApiResponse<LoginResponse>.Fail(
                string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));

        // â”€â”€ Find account â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var normalizedUsername = request.Username.Trim().ToLowerInvariant();
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Username == normalizedUsername, ct);

        if (account is null)
            return ApiResponse<LoginResponse>.Fail("TĂªn Ä‘Äƒng nháº­p hoáº·c máº­t kháº©u khĂ´ng Ä‘Ăºng.");

        // â”€â”€ Check status â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        if (account.Status == AccountStatus.Locked)
            return ApiResponse<LoginResponse>.Fail("TĂ i khoáº£n Ä‘Ă£ bá»‹ khĂ³a. Vui lĂ²ng liĂªn há»‡ quáº£n trá»‹ viĂªn.");

        if (account.Status == AccountStatus.Inactive)
            return ApiResponse<LoginResponse>.Fail("TĂ i khoáº£n Ä‘Ă£ bá»‹ vĂ´ hiá»‡u hĂ³a.");

        // â”€â”€ Verify password â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        if (!BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash))
            return ApiResponse<LoginResponse>.Fail("TĂªn Ä‘Äƒng nháº­p hoáº·c máº­t kháº©u khĂ´ng Ä‘Ăºng.");

        // â”€â”€ Generate tokens â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var accessToken = _jwt.GenerateAccessToken(account);
        var refreshToken = _jwt.GenerateRefreshToken();
        var refreshExpiry = _jwt.GetRefreshTokenExpiry();

        // â”€â”€ Persist login session â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var session = new LoginSession
        {
            AccountId = account.Id,
            RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(refreshToken),
            LoggedInAt = DateTime.UtcNow,
            ExpiresAt = refreshExpiry
        };

        _db.LoginSessions.Add(session);

        // Update last login timestamp
        account.LastLoginAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        // â”€â”€ Build response â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var accessExpiryMinutes = 60; // matches JwtSettings:AccessTokenExpiryMinutes default
        var response = new LoginResponse(
            AccountId: account.Id,
            Username: account.Username,
            FullName: account.FullName,
            Email: account.Email,
            Role: account.Role.ToString(),
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            AccessTokenExpiresAt: DateTime.UtcNow.AddMinutes(accessExpiryMinutes)
        );

        return ApiResponse<LoginResponse>.Ok(response);
    }
}

