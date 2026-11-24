using FluentValidation;

namespace Library.Modules.Core.Identity.Features.VerifyEmailOtp;

public sealed class VerifyEmailOtpValidator : AbstractValidator<VerifyEmailOtpRequest>
{
    public VerifyEmailOtpValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Email không đúng định dạng.");

        RuleFor(x => x.Otp)
            .NotEmpty().WithMessage("Mã xác thực OTP không được để trống.")
            .Length(6).WithMessage("Mã xác thực OTP phải gồm đúng 6 chữ số.")
            .Matches(@"^\d{6}$").WithMessage("Mã xác thực OTP chỉ được chứa chữ số.");
    }
}

