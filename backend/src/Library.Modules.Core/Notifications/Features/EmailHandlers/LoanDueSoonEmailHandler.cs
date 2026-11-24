using Library.Modules.Core.Catalog.Contracts;
using Library.Modules.Core.Identity.Features.GetAccountContact;
using Library.Modules.Core.Notifications.Services;
using Library.Modules.Core.Shared.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Library.Modules.Core.Notifications.Features.EmailHandlers;

public sealed class LoanDueSoonEmailHandler : INotificationHandler<LoanDueSoonEvent>
{
    private readonly ISender _sender;
    private readonly IEmailService _emailService;
    private readonly ILogger<LoanDueSoonEmailHandler> _logger;

    public LoanDueSoonEmailHandler(
        ISender sender,
        IEmailService emailService,
        ILogger<LoanDueSoonEmailHandler> logger)
    {
        _sender = sender;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(LoanDueSoonEvent notification, CancellationToken ct)
    {
        try
        {
            var contact = await _sender.Send(new GetAccountContactQuery(notification.AccountId), ct);
            if (contact is null || string.IsNullOrWhiteSpace(contact.Email))
            {
                _logger.LogWarning("Cannot send loan due soon email: Contact not found for account {AccountId}", notification.AccountId);
                return;
            }

            var bookTitle = "Sách mượn";
            try
            {
                var copies = await _sender.Send(new GetCopyInfosQuery(new[] { (int)notification.BookCopyId }), ct);
                if (copies != null && copies.TryGetValue((int)notification.BookCopyId, out var copyInfo) && !string.IsNullOrWhiteSpace(copyInfo.BookTitle))
                {
                    bookTitle = copyInfo.BookTitle;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve book title for copy {CopyId}", notification.BookCopyId);
            }

            var html = EmailTemplates.BuildLoanDueSoonEmail(contact.FullName, bookTitle, notification.DueDate);
            var result = await _emailService.SendEmailAsync(contact.Email, "[Libri] Nhắc nhở sắp đến hạn trả sách", html, ct);

            if (!result.Success)
            {
                _logger.LogWarning("Failed to send loan due soon email to {Email}: {Error}", contact.Email, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending loan due soon email for account {AccountId}", notification.AccountId);
        }
    }
}

