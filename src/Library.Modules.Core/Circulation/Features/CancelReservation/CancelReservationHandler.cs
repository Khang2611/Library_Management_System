using FluentValidation;
using Library.Modules.Core.Catalog.Contracts;
using Library.Modules.Core.Circulation.Contracts;
using Library.Modules.Core.Circulation.Data;
using Library.Modules.Core.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Library.Modules.Core.Circulation.Features.CancelReservation;

public sealed class CancelReservationHandler : IRequestHandler<CancelReservationRequest, ApiResponse<bool>>
{
    private readonly AppDbContext _db;
    private readonly IValidator<CancelReservationRequest> _validator;
    private readonly ISender _sender;

    public CancelReservationHandler(AppDbContext db, IValidator<CancelReservationRequest> validator, ISender sender)
    {
        _db = db;
        _validator = validator;
        _sender = sender;
    }

    public async Task<ApiResponse<bool>> Handle(CancelReservationRequest request, CancellationToken ct)
    {
        var val = await _validator.ValidateAsync(request, ct);
        if (!val.IsValid) return ApiResponse<bool>.Fail(string.Join("; ", val.Errors.Select(e => e.ErrorMessage)));

        var res = await _db.Reservations.FirstOrDefaultAsync(r => r.Id == request.ReservationId, ct);
        if (res == null) return ApiResponse<bool>.Fail("Yêu cầu đặt trước không tồn tại.");

        // Allow cancellation if waiting or ready
        if (res.Status != ReservationStatus.Waiting && res.Status != ReservationStatus.Ready)
            return ApiResponse<bool>.Fail("Không thể hủy yêu cầu đặt trước ở trạng thái này.");

        res.Status = ReservationStatus.Cancelled;
        res.CancelledAt = DateTime.UtcNow;

        if (res.BookCopyId.HasValue)
        {
            var copyMap = await _sender.Send(new GetCopyInfosQuery(new[] { res.BookCopyId.Value }), ct);
            if (copyMap.TryGetValue(res.BookCopyId.Value, out var copyInfo) && copyInfo.Status == CopyStatus.Reserved)
            {
                var newStatus = await _sender.Send(
                    new ReleaseCopyCommand(res.BookId, res.BookCopyId.Value, res.Id), ct);

                await _sender.Send(
                    new SetCopyStatusCommand(res.BookCopyId.Value, CopyStatus.Reserved, newStatus), ct);
            }
        }

        await _db.SaveChangesAsync(ct);
        return ApiResponse<bool>.Ok(true);
    }
}
