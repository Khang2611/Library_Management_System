using Library.Modules.Core.Catalog.Contracts;
using Library.Modules.Core.Circulation.Contracts;
using Library.Modules.Core.Circulation.Data;
using Library.Modules.Core.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Library.Modules.Core.Circulation.Features.ReleaseCopy;

public sealed class ReleaseCopyHandler : IRequestHandler<ReleaseCopyCommand, CopyStatus>
{
    private readonly AppDbContext _db;

    public ReleaseCopyHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CopyStatus> Handle(ReleaseCopyCommand request, CancellationToken ct)
    {
        var query = _db.Reservations
            .Where(r => r.BookId == request.BookId && r.Status == ReservationStatus.Waiting);

        if (request.ExcludeReservationId.HasValue)
        {
            query = query.Where(r => r.Id != request.ExcludeReservationId.Value);
        }

        var waitingRes = await query
            .OrderBy(r => r.ReservedAt)
            .FirstOrDefaultAsync(ct);

        if (waitingRes != null)
        {
            waitingRes.BookCopyId = request.CopyId;
            waitingRes.Status = ReservationStatus.Ready;
            waitingRes.ExpiresAt = DateTime.UtcNow.AddDays(3);
            return CopyStatus.Reserved;
        }

        return CopyStatus.Available;
    }
}

