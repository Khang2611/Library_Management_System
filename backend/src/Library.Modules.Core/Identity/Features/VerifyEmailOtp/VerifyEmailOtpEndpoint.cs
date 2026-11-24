using Library.Modules.Core.Shared;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Library.Modules.Core.Identity.Features.VerifyEmailOtp;

public static class VerifyEmailOtpEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/identity/verify-email-otp", async (
            [FromBody] VerifyEmailOtpRequest request,
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

