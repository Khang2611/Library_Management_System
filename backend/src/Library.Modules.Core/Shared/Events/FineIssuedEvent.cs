using MediatR;
using Library.Modules.Core.Billing.Contracts;

namespace Library.Modules.Core.Shared.Events;

public record FineIssuedEvent(
    long FineId,
    int AccountId,
    decimal Amount,
    FineType FineType
) : INotification;
