using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using Library.Modules.Core.Identity.Data;
using Library.Modules.Core.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Library.Modules.Core.Identity.Features.ResetPassword;

public sealed class ResetPasswordHandler : IRequestHandler<ResetPasswordRequest, ApiResponse<bool>>
{
    private readonly IdentityDbContext _db;
    private readonly IValidator<ResetPasswordRequest> _validator;

    public ResetPasswordHandler(IdentityDbContext db, IValidator<ResetPasswordRequest> validator)
    {
        _db = db;
        _validator = validator;
    }

    public async Task<ApiResponse<bool>> Handle(ResetPasswordRequest request, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return ApiResponse<bool>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Email == normalizedEmail, ct);
        if (account is null || account.Status != AccountStatus.Active)
            return ApiResponse<bool>.Fail("Yêu cầu đặt lại mật khẩu không hợp lệ.");

        var inputTrimmed = request.TokenOrOtp.Trim();
        var inputHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(inputTrimmed))).ToLowerInvariant();

        var resetRecord = await _db.PasswordResetTokens
            .Where(t => t.AccountId == account.Id && t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(t => t.TokenHash == inputHash || t.OtpHash == inputHash, ct);

        if (resetRecord is null)
            return ApiResponse<bool>.Fail("Mã xác thực hoặc liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");

        // Cập nhật mật khẩu và thu hồi các token cũ
        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        account.UpdatedAt = DateTime.UtcNow;
        resetRecord.UsedAt = DateTime.UtcNow;

        // Hủy tất cả phiên đăng nhập cũ
        var sessions = await _db.LoginSessions.Where(s => s.AccountId == account.Id && s.LoggedOutAt == null).ToListAsync(ct);
        foreach (var s in sessions)
            s.LoggedOutAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return ApiResponse<bool>.Ok(true);
    }
}
