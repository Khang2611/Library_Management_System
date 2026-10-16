using Library.Modules.Core.Shared;
using MediatR;

namespace Library.Modules.Core.Search.Features.LogSearch;

public sealed class LogSearchHandler : IRequestHandler<LogSearchRequest, ApiResponse<long>>
{
    private readonly AppDbContext _db;
    public LogSearchHandler(AppDbContext db) => _db = db;

    public Task<ApiResponse<long>> Handle(LogSearchRequest request, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
