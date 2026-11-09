using FluentValidation;
using Library.Modules.Core.Catalog.Contracts;
using Library.Modules.Core.Circulation.Data;
using Library.Modules.Core.Identity.Contracts;
using Library.Modules.Core.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Library.Modules.Core.Circulation.Features.ReserveBook;

public sealed class ReserveBookHandler : IRequestHandler<ReserveBookRequest, ApiResponse<long>>
{
    private readonly AppDbContext _db;
    private readonly IValidator<ReserveBookRequest> _validator;
    private readonly ISender _sender;

    public ReserveBookHandler(AppDbContext db, IValidator<ReserveBookRequest> validator, ISender sender)
    {
        _db = db;
        _validator = validator;
        _sender = sender;
    }

    public async Task<ApiResponse<long>> Handle(ReserveBookRequest request, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return ApiResponse<long>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));

        var account = await _sender.Send(new GetAccountSummaryQuery(request.AccountId), ct);
        if (account == null)
            return ApiResponse<long>.Fail("Tài khoản không tồn tại.");

        if (!account.IsActive)
            return ApiResponse<long>.Fail("Tài khoản đang bị khóa hoặc ngưng hoạt động, không thể đặt trước sách.");

        var bookMap = await _sender.Send(new GetBookInfosQuery(new[] { request.BookId }), ct);
        if (!bookMap.TryGetValue(request.BookId, out var book) || !book.IsActive)
            return ApiResponse<long>.Fail("Sách không tồn tại.");

        if (request.BookCopyId.HasValue)
        {
            var copyMap = await _sender.Send(new GetCopyInfosQuery(new[] { request.BookCopyId.Value }), ct);
            if (!copyMap.TryGetValue(request.BookCopyId.Value, out var copy) || copy.BookId != request.BookId)
                return ApiResponse<long>.Fail("Bản sao sách không tồn tại hoặc không thuộc về cuốn sách này.");
            
            if (copy.Status != CopyStatus.Available)
                return ApiResponse<long>.Fail("Bản sao sách không sẵn sàng để đặt trước.");
                
            var setSuccess = await _sender.Send(
                new SetCopyStatusCommand(copy.CopyId, CopyStatus.Available, CopyStatus.Reserved), ct);
            if (!setSuccess)
                return ApiResponse<long>.Fail("Bản sao sách không sẵn sàng để đặt trước.");
        }

        var reservation = new Reservation
        {
            AccountId = request.AccountId,
            BookId = request.BookId,
            BookCopyId = request.BookCopyId,
            Status = request.BookCopyId.HasValue ? ReservationStatus.Ready : ReservationStatus.Waiting
        };

        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync(ct);

        return ApiResponse<long>.Ok(reservation.Id);
    }
}
