using Library.Modules.Core.Notifications.Data;
using Library.Modules.Core.Shared;
using Library.Modules.Core.Shared.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Library.Modules.Core.Notifications.Features.EventHandlers;

public class LoanOverdueNotifHandler : INotificationHandler<LoanOverdueEvent>
{
    private readonly AppDbContext _db;

    public LoanOverdueNotifHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task Handle(LoanOverdueEvent notification, CancellationToken ct)
    {
        var todayUtc = DateTime.UtcNow.Date;
        var alreadyNotifiedToday = await _db.Notifications.AnyAsync(
            n => n.AccountId == notification.AccountId &&
                 n.NotificationType == NotificationType.Borrow &&
                 n.CreatedAt >= todayUtc &&
                 n.Title == "Sach qua han tra", ct);

        if (alreadyNotifiedToday) return;

        var notif = new Notification
        {
            AccountId = notification.AccountId,
            Title = "Sach qua han tra",
            Content = $"Ban dang co sach qua han. Han tra: {notification.DueDate:dd/MM/yyyy}. Vui long tra sach som de tranh phat sinh them phi phat.",
            NotificationType = NotificationType.Borrow,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        _db.Notifications.Add(notif);
        await _db.SaveChangesAsync(ct);
    }
}

