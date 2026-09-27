using TheLife.Api.Common;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Results;
using TheLife.Application.Features.Auth;
using TheLife.Application.Features.Users;

namespace TheLife.Api.Endpoints;

/// <summary>Settings of the signed-in user's own account.</summary>
internal static class AccountEndpoints
{
    public static void MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/account").WithTags("Account").RequireAuthorization();

        group.MapPut("/profile", async (
                UpdateProfileCommand command,
                ICommandHandler<UpdateProfileCommand, CurrentUserDto> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(command, cancellationToken)).ToHttpResult());

        group.MapPut("/avatar", async (
                IFormFile image,
                ICommandHandler<UpdateAvatarCommand, CurrentUserDto> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new UpdateAvatarCommand(image.ToFileUpload()), cancellationToken)).ToHttpResult())
            .DisableAntiforgery(); // API uses bearer tokens, not cookies, so CSRF tokens are not needed.

        group.MapDelete("/avatar", async (
                ICommandHandler<UpdateAvatarCommand, CurrentUserDto> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new UpdateAvatarCommand(Image: null), cancellationToken)).ToHttpResult());

        group.MapPost("/change-password", async (
                ChangePasswordCommand command,
                ICommandHandler<ChangePasswordCommand, Unit> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(command, cancellationToken)).ToHttpResult())
            .RequireRateLimiting(ApiServices.AuthRateLimitPolicy);
    }
}
