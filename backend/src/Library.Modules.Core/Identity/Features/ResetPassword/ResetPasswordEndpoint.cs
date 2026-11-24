using Library.Modules.Core.Shared;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Library.Modules.Core.Identity.Features.ResetPassword;

public static class ResetPasswordEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/identity/reset-password", async (
            [FromBody] ResetPasswordRequest request,
            [FromServices] ISender sender) =>
        {
            var result = await sender.Send(request);
            return result.Success
                ? Results.Ok(result)
                : Results.BadRequest(result);
        })
        .WithTags("Identity")
        .AllowAnonymous();
    }
}

