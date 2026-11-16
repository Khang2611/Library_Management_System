using Library.Modules.Core.Billing.Contracts;
using Library.Modules.Core.Billing.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Library.Modules.Core.Billing.Features;

public sealed class BillingQueryHandlers :
    IRequestHandler<HasUnpaidFinesQuery, bool>,
    IRequestHandler<GetUnpaidFinesTotalQuery, decimal>
{
    private readonly BillingDbContext _db;

    public BillingQueryHandlers(BillingDbContext db) => _db = db;

    public async Task<bool> Handle(HasUnpaidFinesQuery request, CancellationToken ct)
    {
        return await _db.Fines
            .AsNoTracking()
            .AnyAsync(f => f.AccountId == request.AccountId
                        && (f.Status == FineStatus.Unpaid || f.Status == FineStatus.PartiallyPaid), ct);
    }

    public async Task<decimal> Handle(GetUnpaidFinesTotalQuery request, CancellationToken ct)
    {
        return await _db.Fines
            .AsNoTracking()
            .Where(f => f.Status == FineStatus.Unpaid || f.Status == FineStatus.PartiallyPaid)
            .SumAsync(f => (decimal?)f.Amount, ct) ?? 0m;
    }
}

