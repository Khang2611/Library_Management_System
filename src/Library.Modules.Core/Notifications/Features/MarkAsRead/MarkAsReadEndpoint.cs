using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Library.Modules.Core.Notifications.Features.MarkAsRead;

public static class MarkAsReadEndpoint
{
    public static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/notifications/{notificationId:long}/read",
            async (long notificationId, MarkAsReadRequest req, IMediator mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(req with { NotificationId = notificationId }, ct)))
           .WithTags("Notifications")
           .WithSummary("Mark a notification as read")
           .RequireAuthorization();

        return app;
    }
}
