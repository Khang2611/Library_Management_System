using Library.Modules.Core.Catalog.Contracts;
using Library.Modules.Core.Identity.Features.GetAccountContact;
using Library.Modules.Core.Notifications.Services;
using Library.Modules.Core.Shared.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Library.Modules.Core.Notifications.Features.EmailHandlers;

public sealed class LoanOverdueEmailHandler : INotificationHandler<LoanOverdueEvent>
{
    private readonly ISender _sender;
    private readonly IEmailService _emailService;
    private readonly ILogger<LoanOverdueEmailHandler> _logger;

    public LoanOverdueEmailHandler(
        ISender sender,
        IEmailService emailService,
        ILogger<LoanOverdueEmailHandler> logger)
    {
        _sender = sender;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(LoanOverdueEvent notification, CancellationToken ct)
    {
        try
        {
            var contact = await _sender.Send(new GetAccountContactQuery(notification.AccountId), ct);
            if (contact is null || string.IsNullOrWhiteSpace(contact.Email))
            {
                _logger.LogWarning("Cannot send loan overdue email: Contact not found for account {AccountId}", notification.AccountId);
                return;
            }

            var bookTitle = "Sách mượn";
            try
            {
                var copies = await _sender.Send(new GetCopyInfosQuery(new[] { notification.BookCopyId }), ct);
                if (copies != null && copies.TryGetValue(notification.BookCopyId, out var copyInfo) && !string.IsNullOrWhiteSpace(copyInfo.BookTitle))
                {
                    bookTitle = copyInfo.BookTitle;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve book title for copy {CopyId}", notification.BookCopyId);
            }

            var html = EmailTemplates.BuildLoanOverdueEmail(contact.FullName, bookTitle, notification.DueDate);
            var result = await _emailService.SendEmailAsync(contact.Email, "[Libri] Thông báo sách mượn quá hạn", html, ct);

            if (!result.Success)
            {
                _logger.LogWarning("Failed to send loan overdue email to {Email}: {Error}", contact.Email, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending loan overdue email for account {AccountId}", notification.AccountId);
        }
    }
}

