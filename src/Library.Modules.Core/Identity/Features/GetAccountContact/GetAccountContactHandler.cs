using Library.Modules.Core.Identity.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Library.Modules.Core.Identity.Features.GetAccountContact;

public sealed record GetAccountContactQuery(int AccountId) : IRequest<AccountContactDto?>;
public sealed record AccountContactDto(int AccountId, string FullName, string Email);

public sealed class GetAccountContactHandler : IRequestHandler<GetAccountContactQuery, AccountContactDto?>
{
    private readonly IdentityDbContext _db;

    public GetAccountContactHandler(IdentityDbContext db) => _db = db;

    public async Task<AccountContactDto?> Handle(GetAccountContactQuery request, CancellationToken ct)
    {
        var account = await _db.Accounts
            .AsNoTracking()
            .Where(a => a.Id == request.AccountId)
            .Select(a => new AccountContactDto(a.Id, a.FullName, a.Email))
            .FirstOrDefaultAsync(ct);

        return account;
    }
}

