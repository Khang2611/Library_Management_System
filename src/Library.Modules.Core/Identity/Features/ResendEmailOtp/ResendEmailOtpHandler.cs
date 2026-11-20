using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using Library.Modules.Core.Identity.Data;
using Library.Modules.Core.Notifications.Services;
using Library.Modules.Core.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Library.Modules.Core.Identity.Features.ResendEmailOtp;

public sealed class ResendEmailOtpHandler : IRequestHandler<ResendEmailOtpRequest, ApiResponse<bool>>
{
    private readonly IdentityDbContext _db;
    private readonly IValidator<ResendEmailOtpRequest> _validator;
    private readonly IEmailService _emailService;

    public ResendEmailOtpHandler(IdentityDbContext db, IValidator<ResendEmailOtpRequest> validator, IEmailService emailService)
    {
        _db = db;
        _validator = validator;
        _emailService = emailService;
    }

    public async Task<ApiResponse<bool>> Handle(ResendEmailOtpRequest request, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return ApiResponse<bool>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Email == normalizedEmail, ct);
        if (account is null || account.Status == AccountStatus.Active || account.Status == AccountStatus.Locked)
            return ApiResponse<bool>.Ok(true); // Anti-enumeration

        // Rate limit 60s
        var latestOtp = await _db.EmailVerificationOtps
            .Where(o => o.AccountId == account.Id)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (latestOtp != null && DateTime.UtcNow < latestOtp.CreatedAt.AddSeconds(60))
        {
            var waitSeconds = (int)Math.Ceiling((latestOtp.CreatedAt.AddSeconds(60) - DateTime.UtcNow).TotalSeconds);
            return ApiResponse<bool>.Fail($"Vui lòng đợi {waitSeconds} giây trước khi yêu cầu gửi lại mã.");
        }

        var previousOtps = await _db.EmailVerificationOtps
            .Where(o => o.AccountId == account.Id && o.UsedAt == null)
            .ToListAsync(ct);

        foreach (var oldOtp in previousOtps)
            oldOtp.UsedAt = DateTime.UtcNow;

        var otpCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
        var otpHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(otpCode))).ToLowerInvariant();

        var newOtp = new EmailVerificationOtp
        {
            AccountId = account.Id,
            Email = account.Email,
            OtpHash = otpHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(3),
            FailedAttempts = 0
        };

        _db.EmailVerificationOtps.Add(newOtp);
        await _db.SaveChangesAsync(ct);

        var emailResult = await _emailService.SendRegistrationOtpEmailAsync(account.Email, otpCode, account.FullName, ct);
        if (emailResult != null && !emailResult.Success)
            return ApiResponse<bool>.Fail($"Không thể gửi email mã xác thực: {emailResult.ErrorMessage}");

        return ApiResponse<bool>.Ok(true);
    }
}

