using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using Library.Modules.Core.Identity.Data;
using Library.Modules.Core.Notifications.Services;
using Library.Modules.Core.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Library.Modules.Core.Identity.Features.ForgotPassword;

public sealed class ForgotPasswordHandler : IRequestHandler<ForgotPasswordRequest, ApiResponse<bool>>
{
    private readonly IdentityDbContext _db;
    private readonly IValidator<ForgotPasswordRequest> _validator;
    private readonly IEmailService _emailService;
    private readonly ILogger<ForgotPasswordHandler>? _logger;

    public ForgotPasswordHandler(
        IdentityDbContext db,
        IValidator<ForgotPasswordRequest> validator,
        IEmailService emailService,
        ILogger<ForgotPasswordHandler>? logger = null)
    {
        _db = db;
        _validator = validator;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(ForgotPasswordRequest request, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return ApiResponse<bool>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Email == normalizedEmail, ct);
        if (account is null || account.Status != AccountStatus.Active)
            return ApiResponse<bool>.Ok(true); // Anti-enumeration

        // Rate limit 60s
        var latestToken = await _db.PasswordResetTokens
            .Where(t => t.AccountId == account.Id)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (latestToken != null && DateTime.UtcNow < latestToken.CreatedAt.AddSeconds(60))
        {
            var waitSeconds = (int)Math.Ceiling((latestToken.CreatedAt.AddSeconds(60) - DateTime.UtcNow).TotalSeconds);
            return ApiResponse<bool>.Fail($"Vui lòng đợi {waitSeconds} giây trước khi yêu cầu đặt lại mật khẩu.");
        }

        // Sinh reset token và OTP 6 số
        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();

        var otpCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
        var otpHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(otpCode))).ToLowerInvariant();

        var resetEntity = new PasswordResetToken
        {
            AccountId = account.Id,
            TokenHash = tokenHash,
            OtpHash = otpHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            CreatedAt = DateTime.UtcNow
        };

        _db.PasswordResetTokens.Add(resetEntity);
        await _db.SaveChangesAsync(ct);

        var emailResult = await _emailService.SendPasswordResetEmailAsync(account.Email, rawToken, otpCode, ct);
        if (emailResult != null && !emailResult.Success)
            return ApiResponse<bool>.Fail($"Lỗi gửi email: {emailResult.ErrorMessage}");

        return ApiResponse<bool>.Ok(true);
    }
}

