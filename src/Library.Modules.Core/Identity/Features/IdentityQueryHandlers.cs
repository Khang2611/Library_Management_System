using Library.Modules.Core.Identity.Contracts;
using Library.Modules.Core.Identity.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Library.Modules.Core.Identity.Features;

public sealed class IdentityQueryHandlers :
    IRequestHandler<GetTotalAccountsCountQuery, int>,
    IRequestHandler<SearchAccountIdsQuery, IReadOnlyList<int>>
{
    private readonly IdentityDbContext _db;

    public IdentityQueryHandlers(IdentityDbContext db) => _db = db;

    public async Task<int> Handle(GetTotalAccountsCountQuery request, CancellationToken ct)
    {
        return await _db.Accounts.AsNoTracking().CountAsync(ct);
    }

    public async Task<IReadOnlyList<int>> Handle(SearchAccountIdsQuery request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Keyword))
            return [];

        var kw = request.Keyword.Trim();
        return await _db.Accounts
            .AsNoTracking()
            .Where(a => a.FullName.Contains(kw) || a.Username.Contains(kw) || a.Email.Contains(kw))
            .Select(a => a.Id)
            .ToListAsync(ct);
    }
}

