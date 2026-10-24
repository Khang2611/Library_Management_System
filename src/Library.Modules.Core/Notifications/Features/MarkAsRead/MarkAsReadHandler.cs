using Library.Modules.Core.Shared;
using MediatR;

namespace Library.Modules.Core.Notifications.Features.MarkAsRead;

public sealed class MarkAsReadHandler : IRequestHandler<MarkAsReadRequest, ApiResponse<bool>>
{
    private readonly AppDbContext _db;
    public MarkAsReadHandler(AppDbContext db) => _db = db;

    public Task<ApiResponse<bool>> Handle(MarkAsReadRequest request, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
