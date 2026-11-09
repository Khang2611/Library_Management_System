using Library.Modules.Core.Catalog.Contracts;
using Library.Modules.Core.Circulation.Contracts;
using Library.Modules.Core.Circulation.Data;
using Library.Modules.Core.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Library.Modules.Core.Circulation.Features.ExpireReservations;

public sealed class ExpireReservationsHandler : IRequestHandler<ExpireReservationsCommand, int>
{
    private readonly AppDbContext _db;
    private readonly ISender _sender;

    public ExpireReservationsHandler(AppDbContext db, ISender sender)
    {
        _db = db;
        _sender = sender;
    }

    public async Task<int> Handle(ExpireReservationsCommand request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var expiredReservations = await _db.Reservations
            .Where(r => r.Status == ReservationStatus.Ready && r.ExpiresAt.HasValue && r.ExpiresAt.Value < now)
            .ToListAsync(ct);

        if (expiredReservations.Count == 0)
            return 0;

        foreach (var res in expiredReservations)
        {
            res.Status = ReservationStatus.Expired;

            if (res.BookCopyId.HasValue)
            {
                var copyMap = await _sender.Send(new GetCopyInfosQuery(new[] { res.BookCopyId.Value }), ct);
                if (copyMap.TryGetValue(res.BookCopyId.Value, out var copyInfo) && copyInfo.Status == CopyStatus.Reserved)
                {
                    var newStatus = await _sender.Send(
                        new ReleaseCopyCommand(res.BookId, res.BookCopyId.Value, res.Id), ct);

                    await _sender.Send(
                        new SetCopyStatusCommand(res.BookCopyId.Value, CopyStatus.Reserved, newStatus), ct);
                }
            }
        }

        await _db.SaveChangesAsync(ct);
        return expiredReservations.Count;
    }
}

