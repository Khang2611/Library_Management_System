using FluentValidation;

namespace Library.Modules.Core.Catalog.Features.ManageCopies;

public sealed class AddCopyValidator : AbstractValidator<AddCopyRequest>
{
    public AddCopyValidator()
    {
        RuleFor(x => x.BookId)
            .GreaterThan(0).WithMessage("BookId không hợp lệ.");

        RuleFor(x => x.Barcode)
            .NotEmpty().WithMessage("Mã vạch không được để trống.")
            .MaximumLength(50).WithMessage("Mã vạch tối đa 50 ký tự.");

        RuleFor(x => x.Location)
            .MaximumLength(100).WithMessage("Vị trí kệ tối đa 100 ký tự.")
            .When(x => !string.IsNullOrEmpty(x.Location));

        RuleFor(x => x.AcquiredDate)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Ngày nhập sách không được sau ngày hiện tại.")
            .When(x => x.AcquiredDate.HasValue);
    }
}
