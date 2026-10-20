using Library.Modules.Core.Circulation.Data;
using Library.Modules.Core.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Library.Modules.Core.Circulation.Features.GetMyLoans;

public sealed class GetMyLoansHandler : IRequestHandler<GetMyLoansRequest, ApiResponse<List<LoanDto>>>
{
    private readonly AppDbContext _db;
    public GetMyLoansHandler(AppDbContext db) => _db = db;

    public async Task<ApiResponse<List<LoanDto>>> Handle(GetMyLoansRequest request, CancellationToken ct)
    {
        var loans = await _db.Set<LoanSlip>()
            .Include(l => l.Details).ThenInclude(d => d.Renewals)
            .Where(l => l.AccountId == request.AccountId)
            .OrderByDescending(l => l.BorrowedAt)
            .AsNoTracking()
            .ToListAsync(ct);

        var result = loans.Select(l => new LoanDto(
            l.Id,
            l.BorrowedAt,
            l.Status.ToString(),
            l.Details.Select(d => new LoanDetailDto(
                d.Id, d.BookCopyId, l.DueDate, d.ActualReturnDate, d.Status.ToString(), d.Renewals.Count
            )).ToList()
        )).ToList();

        return ApiResponse<List<LoanDto>>.Ok(result);
    }
}