using Library.Modules.Core.Shared;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Library.Modules.Core.Identity.Features.ForgotPassword;

public static class ForgotPasswordEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/identity/forgot-password", async (
            [FromBody] ForgotPasswordRequest request,
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

