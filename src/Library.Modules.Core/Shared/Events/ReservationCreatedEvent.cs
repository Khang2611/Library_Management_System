using MediatR;

namespace Library.Modules.Core.Shared.Events;

public sealed record ReservationCreatedEvent(long ReservationId, int AccountId, long BookId, bool IsReady, DateTime? ExpiresAt) : INotification;

