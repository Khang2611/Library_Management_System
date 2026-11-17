using Library.Modules.Core.Search.Data;
using Library.Modules.Core.Shared;
using MediatR;

namespace Library.Modules.Core.Search.Features.LogSearch;

public sealed class LogSearchHandler : IRequestHandler<LogSearchRequest, ApiResponse<long>>
{
    private readonly SearchDbContext _db;
    public LogSearchHandler(SearchDbContext db) => _db = db;

    public Task<ApiResponse<long>> Handle(LogSearchRequest request, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}

