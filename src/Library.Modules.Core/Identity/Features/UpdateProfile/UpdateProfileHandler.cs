using Library.Modules.Core.Identity.Data;
using FluentValidation;
using Library.Modules.Core.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Library.Modules.Core.Identity.Features.UpdateProfile;

public sealed class UpdateProfileHandler : IRequestHandler<UpdateProfileRequest, ApiResponse<bool>>
{
    private readonly IdentityDbContext _db;
    private readonly IValidator<UpdateProfileRequest> _validator;

    public UpdateProfileHandler(IdentityDbContext db, IValidator<UpdateProfileRequest> validator)
    {
        _db = db;
        _validator = validator;
    }

    public async Task<ApiResponse<bool>> Handle(UpdateProfileRequest request, CancellationToken ct)
    {
        // â”€â”€ Validate â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return ApiResponse<bool>.Fail(
                string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));

        // â”€â”€ Find account â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var account = await _db.Accounts.FindAsync([request.AccountId], ct);
        if (account is null)
            return ApiResponse<bool>.Fail("TĂ i khoáº£n khĂ´ng tá»“n táº¡i.");

        // â”€â”€ Check phone uniqueness (if changed) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            var phone = request.PhoneNumber.Trim();
            var phoneInUse = await _db.Accounts
                .AnyAsync(a => a.PhoneNumber == phone && a.Id != request.AccountId, ct);

            if (phoneInUse)
                return ApiResponse<bool>.Fail("Sá»‘ Ä‘iá»‡n thoáº¡i Ä‘Ă£ Ä‘Æ°á»£c sá»­ dá»¥ng bá»Ÿi tĂ i khoáº£n khĂ¡c.");
        }

        // â”€â”€ Update fields â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        account.FullName = request.FullName.Trim();
        account.PhoneNumber = request.PhoneNumber?.Trim();
        account.Address = request.Address?.Trim();
        account.DateOfBirth = request.DateOfBirth;

        await _db.SaveChangesAsync(ct);

        return ApiResponse<bool>.Ok(true);
    }
}

