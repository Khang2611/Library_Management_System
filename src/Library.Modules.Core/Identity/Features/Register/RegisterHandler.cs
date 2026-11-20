using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using Library.Modules.Core.Identity.Data;
using Library.Modules.Core.Notifications.Services;
using Library.Modules.Core.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Library.Modules.Core.Identity.Features.Register;

public sealed class RegisterHandler : IRequestHandler<RegisterRequest, ApiResponse<RegisterResponse>>
{
    private readonly IdentityDbContext _db;
    private readonly IValidator<RegisterRequest> _validator;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _cfg;
    private readonly ILogger<RegisterHandler>? _logger;

    public RegisterHandler(
        IdentityDbContext db,
        IValidator<RegisterRequest> validator,
        IEmailService emailService,
        IConfiguration cfg,
        ILogger<RegisterHandler>? logger = null)
    {
        _db = db;
        _validator = validator;
        _emailService = emailService;
        _cfg = cfg;
        _logger = logger;
    }

    public async Task<ApiResponse<RegisterResponse>> Handle(RegisterRequest request, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return ApiResponse<RegisterResponse>.Fail(
                string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));

        var normalizedUsername = request.Username.Trim().ToLowerInvariant();
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var phone = request.PhoneNumber?.Trim();

        var secretKey = _cfg["JwtSettings:SecretKey"]
            ?? _cfg["JWT_SECRET_KEY"]
            ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
            ?? "Libri_Default_Secret_Key_For_Session_Token_Verification_2026_AtLeast32Chars!";

        Account? sessionAccount = null;
        if (!string.IsNullOrWhiteSpace(request.RegistrationToken))
        {
            if (RegistrationTokenHelper.TryValidateToken(request.RegistrationToken, secretKey, out var tokenId, out _))
            {
                sessionAccount = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == tokenId, ct);
            }
            if (sessionAccount == null || sessionAccount.Status != AccountStatus.Inactive)
            {
                sessionAccount = null;
            }
        }

        // Chặn trùng lặp với các tài khoản Active
        if (await _db.Accounts.AnyAsync(a => a.Username == normalizedUsername && a.Status == AccountStatus.Active && (sessionAccount == null || a.Id != sessionAccount.Id), ct))
            return ApiResponse<RegisterResponse>.Fail("Tên đăng nhập đã được sử dụng.");

        if (await _db.Accounts.AnyAsync(a => a.Email == normalizedEmail && a.Status == AccountStatus.Active && (sessionAccount == null || a.Id != sessionAccount.Id), ct))
            return ApiResponse<RegisterResponse>.Fail("Email đã được sử dụng.");

        if (!string.IsNullOrEmpty(phone))
        {
            if (await _db.Accounts.AnyAsync(a => a.PhoneNumber == phone && a.Status == AccountStatus.Active && (sessionAccount == null || a.Id != sessionAccount.Id), ct))
                return ApiResponse<RegisterResponse>.Fail("Số điện thoại đã được sử dụng.");
        }

        // Kiểm tra tài khoản Inactive chưa hoàn tất xác thực
        if (sessionAccount == null)
        {
            var pendingInactive = await _db.Accounts
                .FirstOrDefaultAsync(a => (a.Username == normalizedUsername || a.Email == normalizedEmail) && a.Status == AccountStatus.Inactive, ct);
            if (pendingInactive != null)
            {
                sessionAccount = pendingInactive;
            }
        }

        // Rate limit: 60s
        if (sessionAccount != null)
        {
            var lastOtp = await _db.EmailVerificationOtps
                .Where(o => o.AccountId == sessionAccount.Id)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (lastOtp != null && DateTime.UtcNow < lastOtp.CreatedAt.AddSeconds(60))
            {
                var waitSeconds = (int)Math.Ceiling((lastOtp.CreatedAt.AddSeconds(60) - DateTime.UtcNow).TotalSeconds);
                return ApiResponse<RegisterResponse>.Fail($"Vui lòng đợi {waitSeconds} giây trước khi gửi lại yêu cầu đăng ký.");
            }
        }

        // Khởi tạo hoặc cập nhật tài khoản Inactive
        Account account;
        if (sessionAccount != null)
        {
            sessionAccount.Username = normalizedUsername;
            sessionAccount.Email = normalizedEmail;
            sessionAccount.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            sessionAccount.FullName = request.FullName.Trim();
            sessionAccount.PhoneNumber = request.PhoneNumber?.Trim();
            sessionAccount.UpdatedAt = DateTime.UtcNow;
            account = sessionAccount;
        }
        else
        {
            account = new Account
            {
                Username = normalizedUsername,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FullName = request.FullName.Trim(),
                Email = normalizedEmail,
                PhoneNumber = request.PhoneNumber?.Trim(),
                Role = AccountRole.Reader,
                Status = AccountStatus.Inactive,
                CreatedAt = DateTime.UtcNow
            };
            _db.Accounts.Add(account);
        }

        await _db.SaveChangesAsync(ct);

        // Vô hiệu hóa OTP cũ
        var pendingOtps = await _db.EmailVerificationOtps
            .Where(o => o.AccountId == account.Id && o.UsedAt == null)
            .ToListAsync(ct);

        foreach (var oldOtp in pendingOtps)
        {
            oldOtp.UsedAt = DateTime.UtcNow;
        }

        // Sinh mã OTP 6 số
        var otpCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
        var otpHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(otpCode))).ToLowerInvariant();

        var otpEntity = new EmailVerificationOtp
        {
            AccountId = account.Id,
            Email = account.Email,
            OtpHash = otpHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(3),
            FailedAttempts = 0
        };

        _db.EmailVerificationOtps.Add(otpEntity);
        await _db.SaveChangesAsync(ct);

        var sessionToken = RegistrationTokenHelper.GenerateToken(account.Id, account.Email, secretKey);

        // Gửi email OTP
        var emailResult = await _emailService.SendRegistrationOtpEmailAsync(account.Email, otpCode, account.FullName, ct);
        if (emailResult != null && !emailResult.Success)
        {
            _logger?.LogWarning("Account {AccountId} OTP email dispatch failed: {Error}", account.Id, emailResult.ErrorMessage);
            return ApiResponse<RegisterResponse>.Ok(new RegisterResponse(
                AccountId: account.Id,
                Email: account.Email,
                Message: $"Tài khoản đã được tạo nhưng chưa thể gửi email mã xác thực ({emailResult.ErrorMessage}). Vui lòng bấm 'Gửi lại mã' để thử lại.",
                RegistrationToken: sessionToken,
                EmailSent: false,
                EmailError: emailResult.ErrorMessage
            ));
        }

        return ApiResponse<RegisterResponse>.Ok(new RegisterResponse(
            AccountId: account.Id,
            Email: account.Email,
            Message: "Mã xác thực gồm 6 chữ số đã được gửi đến email của bạn.",
            RegistrationToken: sessionToken,
            EmailSent: true
        ));
    }
}
