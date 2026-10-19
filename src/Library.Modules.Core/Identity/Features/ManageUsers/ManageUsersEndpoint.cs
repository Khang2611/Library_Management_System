using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Library.Modules.Core.Identity.Features.ManageUsers;

public static class ManageUsersEndpoint
{
    public static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/identity/users").WithTags("Identity Admin");

        group.MapGet("/", async (IMediator mediator, string? keyword, int? role, int page = 1, int pageSize = 10, CancellationToken ct = default) =>
            Results.Ok(await mediator.Send(new GetUsersRequest(keyword, role, page, pageSize), ct)))
            .WithSummary("Get list of users (Staff & Admin)")
            .RequireAuthorization("StaffOnly");

        group.MapPatch("/{id:int}/role", async (int id, ChangeRoleRequest req, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(req with { AccountId = id }, ct)))
            .WithSummary("Change user role (Admin only)")
            .RequireAuthorization("AdminOnly");

        group.MapPatch("/{id:int}/status", async (int id, ChangeStatusRequest req, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(req with { AccountId = id }, ct)))
            .WithSummary("Change user status (Admin only)")
            .RequireAuthorization("AdminOnly");

        return app;
    }
}