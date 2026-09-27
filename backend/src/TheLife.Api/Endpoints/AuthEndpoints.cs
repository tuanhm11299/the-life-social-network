using TheLife.Api.Common;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Results;
using TheLife.Application.Features.Auth;

namespace TheLife.Api.Endpoints;

/// <summary>What the browser receives after register / login / refresh. The refresh token goes into a cookie instead.</summary>
public sealed record AuthResponse(string AccessToken, DateTime AccessTokenExpiresAt, CurrentUserDto User);

internal static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", async (
                RegisterCommand command,
                ICommandHandler<RegisterCommand, AuthResult> handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(command, cancellationToken);
                return result.ToHttpResult(session => SignIn(httpContext, session));
            })
            .RequireRateLimiting(ApiServices.AuthRateLimitPolicy);

        group.MapPost("/login", async (
                LoginCommand command,
                ICommandHandler<LoginCommand, AuthResult> handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(command, cancellationToken);
                return result.ToHttpResult(session => SignIn(httpContext, session));
            })
            .RequireRateLimiting(ApiServices.AuthRateLimitPolicy);

        // Called by the frontend when the access token expired (and once when the app starts).
        group.MapPost("/refresh", async (
            ICommandHandler<RefreshSessionCommand, AuthResult> handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var refreshToken = RefreshTokenCookie.Read(httpContext) ?? "";
            var result = await handler.Handle(new RefreshSessionCommand(refreshToken), cancellationToken);

            if (result.IsFailure) RefreshTokenCookie.Delete(httpContext);
            return result.ToHttpResult(session => SignIn(httpContext, session));
        });

        group.MapPost("/logout", async (
            ICommandHandler<LogoutCommand, Unit> handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new LogoutCommand(RefreshTokenCookie.Read(httpContext)), cancellationToken);
            RefreshTokenCookie.Delete(httpContext);
            return result.ToHttpResult();
        });

        group.MapGet("/me", async (
                IQueryHandler<GetCurrentUserQuery, CurrentUserDto> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new GetCurrentUserQuery(), cancellationToken)).ToHttpResult())
            .RequireAuthorization();
    }

    private static IResult SignIn(HttpContext httpContext, AuthResult session)
    {
        RefreshTokenCookie.Write(httpContext, session.RefreshToken, session.RefreshTokenExpiresAt);
        return Results.Ok(new AuthResponse(session.AccessToken, session.AccessTokenExpiresAt, session.User));
    }
}
