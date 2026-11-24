using FluentValidation;

namespace Library.Modules.Core.Identity.Features.ResetPassword;

public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Email không đúng định dạng.");

        RuleFor(x => x.TokenOrOtp)
            .NotEmpty().WithMessage("Mã xác thực hoặc mã bảo mật không được để trống.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Mật khẩu mới không được để trống.")
            .MinimumLength(8).WithMessage("Mật khẩu mới phải có ít nhất 8 ký tự.")
            .Matches(@"[A-Z]").WithMessage("Mật khẩu phải chứa ít nhất một chữ in hoa.")
            .Matches(@"[a-z]").WithMessage("Mật khẩu phải chứa ít nhất một chữ in thường.")
            .Matches(@"[0-9]").WithMessage("Mật khẩu phải chứa ít nhất một chữ số.")
            .Matches(@"[\W_]").WithMessage("Mật khẩu phải chứa ít nhất một ký tự đặc biệt.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("Mật khẩu xác nhận không khớp.");
    }
}

