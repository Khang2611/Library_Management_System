using MediatR;

namespace Library.Modules.Core.Shared.Events;

public sealed record AccountLockedEvent(int AccountId, string Reason) : INotification;

