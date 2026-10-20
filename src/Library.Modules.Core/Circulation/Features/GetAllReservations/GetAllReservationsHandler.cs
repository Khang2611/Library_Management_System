using Library.Modules.Core.Circulation.Data;
using Library.Modules.Core.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Library.Modules.Core.Circulation.Features.GetAllReservations;

public sealed class GetAllReservationsHandler : IRequestHandler<GetAllReservationsRequest, ApiResponse<GetAllReservationsResult>>
{
    private readonly AppDbContext _db;
    public GetAllReservationsHandler(AppDbContext db) => _db = db;

    public async Task<ApiResponse<GetAllReservationsResult>> Handle(GetAllReservationsRequest request, CancellationToken ct)
    {
        IQueryable<Reservation> query = _db.Reservations.AsNoTracking();

        if (request.Status.HasValue && Enum.IsDefined(typeof(ReservationStatus), request.Status.Value))
            query = query.Where(r => r.Status == (ReservationStatus)request.Status.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.ReservedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new ReservationSummaryDto(r.Id, r.AccountId, r.BookId, r.BookCopyId, r.Status.ToString(), r.ReservedAt))
            .ToListAsync(ct);

        return ApiResponse<GetAllReservationsResult>.Ok(new GetAllReservationsResult(items, total));
    }
}
