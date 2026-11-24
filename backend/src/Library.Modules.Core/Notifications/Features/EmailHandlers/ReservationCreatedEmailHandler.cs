using Library.Modules.Core.Catalog.Contracts;
using Library.Modules.Core.Identity.Features.GetAccountContact;
using Library.Modules.Core.Notifications.Services;
using Library.Modules.Core.Shared.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Library.Modules.Core.Notifications.Features.EmailHandlers;

public sealed class ReservationCreatedEmailHandler : INotificationHandler<ReservationCreatedEvent>
{
    private readonly ISender _sender;
    private readonly IEmailService _emailService;
    private readonly ILogger<ReservationCreatedEmailHandler> _logger;

    public ReservationCreatedEmailHandler(
        ISender sender,
        IEmailService emailService,
        ILogger<ReservationCreatedEmailHandler> logger)
    {
        _sender = sender;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(ReservationCreatedEvent notification, CancellationToken ct)
    {
        try
        {
            var contact = await _sender.Send(new GetAccountContactQuery(notification.AccountId), ct);
            if (contact is null || string.IsNullOrWhiteSpace(contact.Email))
            {
                _logger.LogWarning("Cannot send reservation created email: Contact not found for account {AccountId}", notification.AccountId);
                return;
            }

            var bookTitle = "Sách đặt trước";
            try
            {
                var books = await _sender.Send(new GetBookInfosQuery(new[] { (int)notification.BookId }), ct);
                if (books != null && books.TryGetValue((int)notification.BookId, out var bookInfo) && !string.IsNullOrWhiteSpace(bookInfo.Title))
                {
                    bookTitle = bookInfo.Title;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve book title for book {BookId}", notification.BookId);
            }

            var html = EmailTemplates.BuildReservationCreatedEmail(contact.FullName, bookTitle, notification.IsReady, notification.ExpiresAt);
            var result = await _emailService.SendEmailAsync(contact.Email, "[Libri] Xác nhận đặt giữ sách", html, ct);

            if (!result.Success)
            {
                _logger.LogWarning("Failed to send reservation created email to {Email}: {Error}", contact.Email, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending reservation created email for account {AccountId}", notification.AccountId);
        }
    }
}

