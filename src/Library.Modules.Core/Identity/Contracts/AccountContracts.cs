using MediatR;

namespace Library.Modules.Core.Identity.Contracts;

public record AccountSummary(int Id, string FullName, bool IsActive, string? Username = null);
public record GetAccountSummaryQuery(int AccountId) : IRequest<AccountSummary?>;
public record GetAccountNamesQuery(IReadOnlyCollection<int> AccountIds)
    : IRequest<IReadOnlyDictionary<int, string>>;
public record SeedDefaultAdminCommand : IRequest<bool>;
public record GetTotalAccountsCountQuery : IRequest<int>;
public record SearchAccountIdsQuery(string Keyword) : IRequest<IReadOnlyList<int>>;

