using Library.Modules.Core.Identity.Features.GetAccountContact;
using Library.Modules.Core.Notifications.Services;
using Library.Modules.Core.Shared.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Library.Modules.Core.Notifications.Features.EmailHandlers;

public sealed class AccountLockedEmailHandler : INotificationHandler<AccountLockedEvent>
{
    private readonly ISender _sender;
    private readonly IEmailService _emailService;
    private readonly ILogger<AccountLockedEmailHandler> _logger;

    public AccountLockedEmailHandler(
        ISender sender,
        IEmailService emailService,
        ILogger<AccountLockedEmailHandler> logger)
    {
        _sender = sender;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(AccountLockedEvent notification, CancellationToken ct)
    {
        try
        {
            var contact = await _sender.Send(new GetAccountContactQuery(notification.AccountId), ct);
            if (contact is null || string.IsNullOrWhiteSpace(contact.Email))
            {
                _logger.LogWarning("Cannot send account locked email: Contact not found for account {AccountId}", notification.AccountId);
                return;
            }

            var html = EmailTemplates.BuildAccountLockedEmail(contact.FullName, notification.Reason);
            var result = await _emailService.SendEmailAsync(contact.Email, "[Libri] Thông báo khóa tài khoản", html, ct);

            if (!result.Success)
            {
                _logger.LogWarning("Failed to send account locked email to {Email}: {Error}", contact.Email, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending account locked email for account {AccountId}", notification.AccountId);
        }
    }
}

