using MediatR;

namespace Library.Modules.Core.Shared.Events;

public sealed record LoanDueSoonEvent(long LoanSlipDetailId, int AccountId, long BookCopyId, DateOnly DueDate) : INotification;

