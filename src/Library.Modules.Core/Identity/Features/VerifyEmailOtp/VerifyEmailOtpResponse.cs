namespace Library.Modules.Core.Identity.Features.VerifyEmailOtp;

public sealed record VerifyEmailOtpResponse(
    int AccountId,
    string Username,
    string FullName,
    string Email,
    string Role,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt
);

