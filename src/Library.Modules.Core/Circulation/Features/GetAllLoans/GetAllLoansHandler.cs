using Library.Modules.Core.Circulation.Data;
using Library.Modules.Core.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Library.Modules.Core.Circulation.Features.GetAllLoans;

public sealed class GetAllLoansHandler : IRequestHandler<GetAllLoansRequest, ApiResponse<GetAllLoansResult>>
{
    private readonly AppDbContext _db;
    public GetAllLoansHandler(AppDbContext db) => _db = db;

    public async Task<ApiResponse<GetAllLoansResult>> Handle(GetAllLoansRequest request, CancellationToken ct)
    {
        IQueryable<LoanSlip> query = _db.LoanSlips.Include(l => l.Details).AsNoTracking();

        if (request.Status.HasValue && Enum.IsDefined(typeof(LoanStatus), request.Status.Value))
            query = query.Where(l => l.Status == (LoanStatus)request.Status.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(l => l.BorrowedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(l => new LoanSummaryDto(l.Id, l.AccountId, l.LibrarianId, l.BorrowedAt, l.Status.ToString(), l.Details.Count))
            .ToListAsync(ct);

        return ApiResponse<GetAllLoansResult>.Ok(new GetAllLoansResult(items, total));
    }
}
