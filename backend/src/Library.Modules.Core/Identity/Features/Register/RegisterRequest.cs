namespace Library.Modules.Core.Identity.Features.Register;

public record RegisterRequest(
    string Username,
    string Password,
    string FullName,
    string Email,
    string? PhoneNumber = null,
    string? RegistrationToken = null
) : MediatR.IRequest<Shared.ApiResponse<RegisterResponse>>;
