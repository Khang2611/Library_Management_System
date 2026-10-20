using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Library.Modules.Core.Circulation.Features.GetAllReservations;

public static class GetAllReservationsEndpoint
{
    public static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/circulation/reservations", async (int? status, int page, int pageSize, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetAllReservationsRequest(status, page, pageSize), ct)))
            .WithTags("Circulation Admin")
            .WithSummary("Get all reservations (Staff only)")
            .RequireAuthorization("StaffOnly");
        return app;
    }
}