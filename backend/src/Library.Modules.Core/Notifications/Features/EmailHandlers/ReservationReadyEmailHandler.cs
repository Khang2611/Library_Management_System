using Library.Modules.Core.Catalog.Contracts;
using Library.Modules.Core.Identity.Features.GetAccountContact;
using Library.Modules.Core.Notifications.Services;
using Library.Modules.Core.Shared.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Library.Modules.Core.Notifications.Features.EmailHandlers;

public sealed class ReservationReadyEmailHandler : INotificationHandler<ReservationReadyEvent>
{
    private readonly ISender _sender;
    private readonly IEmailService _emailService;
    private readonly ILogger<ReservationReadyEmailHandler> _logger;

    public ReservationReadyEmailHandler(
        ISender sender,
        IEmailService emailService,
        ILogger<ReservationReadyEmailHandler> logger)
    {
        _sender = sender;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(ReservationReadyEvent notification, CancellationToken ct)
    {
        try
        {
            var contact = await _sender.Send(new GetAccountContactQuery(notification.AccountId), ct);
            if (contact is null || string.IsNullOrWhiteSpace(contact.Email))
            {
                _logger.LogWarning("Cannot send reservation ready email: Contact not found for account {AccountId}", notification.AccountId);
                return;
            }

            var bookTitle = "Sách đặt trước";
            if (notification.BookCopyId.HasValue)
            {
                try
                {
                    var copies = await _sender.Send(new GetCopyInfosQuery(new[] { notification.BookCopyId.Value }), ct);
                    if (copies != null && copies.TryGetValue(notification.BookCopyId.Value, out var copyInfo) && !string.IsNullOrWhiteSpace(copyInfo.BookTitle))
                    {
                        bookTitle = copyInfo.BookTitle;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to resolve book title for copy {CopyId}", notification.BookCopyId);
                }
            }

            var html = EmailTemplates.BuildReservationReadyEmail(contact.FullName, bookTitle, notification.ExpiresAt);
            var result = await _emailService.SendEmailAsync(contact.Email, "[Libri] Sách đặt trước đã sẵn sàng nhận", html, ct);

            if (!result.Success)
            {
                _logger.LogWarning("Failed to send reservation ready email to {Email}: {Error}", contact.Email, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending reservation ready email for account {AccountId}", notification.AccountId);
        }
    }
}

