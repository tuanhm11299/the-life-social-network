using TheLife.Api.Common;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Pagination;
using TheLife.Application.Features.Follows;
using TheLife.Application.Features.Posts;
using TheLife.Application.Features.Stories;
using TheLife.Application.Features.Users;

namespace TheLife.Api.Endpoints;

/// <summary>Profiles, search, suggestions and following. Users are addressed by username, like instagram.com/jane.</summary>
internal static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users").RequireAuthorization();

        group.MapGet("/search", async (
                string? q,
                IQueryHandler<SearchUsersQuery, IReadOnlyList<UserListItemDto>> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new SearchUsersQuery(q), cancellationToken)).ToHttpResult());

        group.MapGet("/suggestions", async (
                int? limit,
                IQueryHandler<GetSuggestedUsersQuery, IReadOnlyList<UserListItemDto>> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new GetSuggestedUsersQuery(limit), cancellationToken)).ToHttpResult());

        group.MapGet("/{username}", async (
                string username,
                IQueryHandler<GetProfileQuery, ProfileDto> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new GetProfileQuery(username), cancellationToken)).ToHttpResult());

        group.MapGet("/{username}/posts", async (
                string username,
                string? cursor,
                int? pageSize,
                IQueryHandler<GetUserPostsQuery, CursorPage<PostDto>> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new GetUserPostsQuery(username, cursor, pageSize), cancellationToken)).ToHttpResult());

        group.MapGet("/{username}/stories", async (
                string username,
                IQueryHandler<GetUserStoriesQuery, StoryTrayItemDto> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new GetUserStoriesQuery(username), cancellationToken)).ToHttpResult());

        group.MapGet("/{username}/followers", async (
                string username,
                string? cursor,
                int? pageSize,
                IQueryHandler<GetFollowListQuery, CursorPage<UserListItemDto>> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new GetFollowListQuery(username, FollowListKind.Followers, cursor, pageSize), cancellationToken))
                .ToHttpResult());

        group.MapGet("/{username}/following", async (
                string username,
                string? cursor,
                int? pageSize,
                IQueryHandler<GetFollowListQuery, CursorPage<UserListItemDto>> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new GetFollowListQuery(username, FollowListKind.Following, cursor, pageSize), cancellationToken))
                .ToHttpResult());

        group.MapPost("/{username}/follow", async (
                string username,
                ICommandHandler<FollowUserCommand, FollowStatusDto> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new FollowUserCommand(username), cancellationToken)).ToHttpResult());

        group.MapDelete("/{username}/follow", async (
                string username,
                ICommandHandler<UnfollowUserCommand, FollowStatusDto> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new UnfollowUserCommand(username), cancellationToken)).ToHttpResult());
    }
}
