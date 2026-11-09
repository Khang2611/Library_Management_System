using FluentValidation;
using Library.Modules.Core.Catalog.Contracts;
using Library.Modules.Core.Circulation.Contracts;
using Library.Modules.Core.Circulation.Data;
using Library.Modules.Core.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Library.Modules.Core.Circulation.Features.ReturnBook;

public sealed class ReturnBookHandler : IRequestHandler<ReturnBookRequest, ApiResponse<bool>>
{
    private readonly AppDbContext _db;
    private readonly IValidator<ReturnBookRequest> _validator;
    private readonly ISender _sender;

    public ReturnBookHandler(AppDbContext db, IValidator<ReturnBookRequest> validator, ISender sender)
    {
        _db = db;
        _validator = validator;
        _sender = sender;
    }

    public async Task<ApiResponse<bool>> Handle(ReturnBookRequest request, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return ApiResponse<bool>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));

        var loanSlip = await _db.LoanSlips
            .Include(l => l.Details)
            .FirstOrDefaultAsync(l => l.Id == request.LoanSlipId, ct);

        if (loanSlip == null)
            return ApiResponse<bool>.Fail("Phiếu mượn không tồn tại.");

        var copyIdsToUpdate = loanSlip.Details
            .Where(d => request.ReturnedCopies.Any(r => r.LoanSlipDetailId == d.Id))
            .Select(d => d.BookCopyId)
            .ToList();

        var copyMap = await _sender.Send(new GetCopyInfosQuery(copyIdsToUpdate), ct);

        foreach (var ret in request.ReturnedCopies)
        {
            var detail = loanSlip.Details.FirstOrDefault(d => d.Id == ret.LoanSlipDetailId);
            if (detail != null && detail.Status == LoanDetailStatus.Borrowing)
            {
                detail.Status = LoanDetailStatus.Returned;
                detail.ActualReturnDate = DateOnly.FromDateTime(DateTime.UtcNow);
                detail.Notes = ret.Notes;

                if (copyMap.TryGetValue(detail.BookCopyId, out var copy))
                {
                    if (copy.Condition == CopyCondition.MajorDamage)
                    {
                        await _sender.Send(new SetCopyStatusCommand(
                            copy.CopyId,
                            null,
                            CopyStatus.Maintenance,
                            copy.Condition), ct);
                    }
                    else
                    {
                        var newStatus = await _sender.Send(
                            new ReleaseCopyCommand(copy.BookId, copy.CopyId, null), ct);

                        await _sender.Send(new SetCopyStatusCommand(
                            copy.CopyId,
                            null,
                            newStatus,
                            copy.Condition), ct);
                    }
                }
            }
        }

        // Check if all details are returned
        if (loanSlip.Details.All(d => d.Status != LoanDetailStatus.Borrowing))
        {
            loanSlip.Status = LoanStatus.Returned;
            loanSlip.ReturnedDate = DateOnly.FromDateTime(DateTime.UtcNow);
        }

        await _db.SaveChangesAsync(ct);
        return ApiResponse<bool>.Ok(true);
    }
}
