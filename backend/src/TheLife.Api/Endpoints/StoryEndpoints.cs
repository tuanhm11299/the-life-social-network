using TheLife.Api.Common;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Results;
using TheLife.Application.Features.Stories;
using TheLife.Application.Features.Users;

namespace TheLife.Api.Endpoints;

internal static class StoryEndpoints
{
    public static void MapStoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stories").WithTags("Stories").RequireAuthorization();

        group.MapGet("/", async (
                IQueryHandler<GetStoryTrayQuery, IReadOnlyList<StoryTrayItemDto>> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new GetStoryTrayQuery(), cancellationToken)).ToHttpResult());

        // multipart/form-data with one "image" file.
        group.MapPost("/", async (
                IFormFile image,
                ICommandHandler<CreateStoryCommand, StoryDto> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new CreateStoryCommand(image.ToFileUpload()), cancellationToken))
                .ToHttpResult(story => Results.Created($"/api/stories/{story.Id}", story)))
            .DisableAntiforgery(); // API uses bearer tokens, not cookies, so CSRF tokens are not needed.

        group.MapDelete("/{storyId:guid}", async (
                Guid storyId,
                ICommandHandler<DeleteStoryCommand, Unit> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new DeleteStoryCommand(storyId), cancellationToken)).ToHttpResult());

        group.MapPost("/{storyId:guid}/view", async (
                Guid storyId,
                ICommandHandler<MarkStoryViewedCommand, Unit> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new MarkStoryViewedCommand(storyId), cancellationToken)).ToHttpResult());

        group.MapGet("/{storyId:guid}/viewers", async (
                Guid storyId,
                IQueryHandler<GetStoryViewersQuery, IReadOnlyList<UserListItemDto>> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new GetStoryViewersQuery(storyId), cancellationToken)).ToHttpResult());
    }
}
