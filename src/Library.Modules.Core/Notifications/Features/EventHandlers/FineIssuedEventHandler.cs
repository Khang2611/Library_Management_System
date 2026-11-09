using Library.Modules.Core.Notifications.Data;
using Library.Modules.Core.Shared;
using Library.Modules.Core.Shared.Events;
using MediatR;

namespace Library.Modules.Core.Notifications.Features.EventHandlers;

public class FineIssuedEventHandler : INotificationHandler<FineIssuedEvent>
{
    private readonly AppDbContext _db;

    public FineIssuedEventHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task Handle(FineIssuedEvent notification, CancellationToken ct)
    {
        var notif = new Notification
        {
            AccountId = notification.AccountId,
            Title = "Thông báo tiền phạt",
            Content = $"Bạn có một khoản phạt mới ({notification.FineType}) với số tiền {notification.Amount:N0} VND.",
            NotificationType = NotificationType.Fine,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        _db.Notifications.Add(notif);
        await _db.SaveChangesAsync(ct);
    }
}

