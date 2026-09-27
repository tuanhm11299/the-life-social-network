using TheLife.Api.Common;
using TheLife.Application.Common.Files;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Pagination;
using TheLife.Application.Common.Results;
using TheLife.Application.Features.Comments;
using TheLife.Application.Features.Likes;
using TheLife.Application.Features.Posts;
using TheLife.Application.Features.SavedPosts;
using TheLife.Application.Features.Users;
using TheLife.Domain.Posts;
using Microsoft.AspNetCore.Mvc;

namespace TheLife.Api.Endpoints;

public sealed record EditPostRequest(string? Caption);
public sealed record AddCommentRequest(string Text);

/// <summary>Posts plus everything you can do to a post: like, save and comment.</summary>
internal static class PostEndpoints
{
    public static void MapPostEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/posts").WithTags("Posts").RequireAuthorization();

        // ---- Lists ----------------------------------------------------------------------------

        group.MapGet("/feed", async (
                string? cursor,
                int? pageSize,
                IQueryHandler<GetFeedQuery, CursorPage<PostDto>> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new GetFeedQuery(cursor, pageSize), cancellationToken)).ToHttpResult());

        group.MapGet("/explore", async (
                string? cursor,
                int? pageSize,
                IQueryHandler<GetExploreQuery, CursorPage<PostDto>> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new GetExploreQuery(cursor, pageSize), cancellationToken)).ToHttpResult());

        group.MapGet("/saved", async (
                string? cursor,
                int? pageSize,
                IQueryHandler<GetSavedPostsQuery, CursorPage<PostDto>> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new GetSavedPostsQuery(cursor, pageSize), cancellationToken)).ToHttpResult());

        // ---- A single post --------------------------------------------------------------------

        // multipart/form-data with a "caption" field and one or more "images" files.
        group.MapPost("/", async (
                [FromForm] string? caption,
                IFormFileCollection images,
                ICommandHandler<CreatePostCommand, PostDto> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new CreatePostCommand(caption, images.Select(file => file.ToFileUpload()).ToList());
                var result = await handler.Handle(command, cancellationToken);
                return result.ToHttpResult(post => Results.Created($"/api/posts/{post.Id}", post));
            })
            .DisableAntiforgery() // API uses bearer tokens, not cookies, so CSRF tokens are not needed.
            .WithMetadata(new RequestSizeLimitAttribute(Post.MaxImages * ImageRules.MaxSizeInBytes + 1024 * 1024));

        group.MapGet("/{postId:guid}", async (
                Guid postId,
                IQueryHandler<GetPostQuery, PostDto> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new GetPostQuery(postId), cancellationToken)).ToHttpResult());

        group.MapPatch("/{postId:guid}", async (
                Guid postId,
                EditPostRequest request,
                ICommandHandler<EditPostCommand, PostDto> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new EditPostCommand(postId, request.Caption), cancellationToken)).ToHttpResult());

        group.MapDelete("/{postId:guid}", async (
                Guid postId,
                ICommandHandler<DeletePostCommand, Unit> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new DeletePostCommand(postId), cancellationToken)).ToHttpResult());

        // ---- Likes ----------------------------------------------------------------------------

        group.MapPost("/{postId:guid}/like", async (
                Guid postId,
                ICommandHandler<LikePostCommand, LikeStatusDto> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new LikePostCommand(postId), cancellationToken)).ToHttpResult());

        group.MapDelete("/{postId:guid}/like", async (
                Guid postId,
                ICommandHandler<UnlikePostCommand, LikeStatusDto> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new UnlikePostCommand(postId), cancellationToken)).ToHttpResult());

        group.MapGet("/{postId:guid}/likes", async (
                Guid postId,
                IQueryHandler<GetPostLikesQuery, IReadOnlyList<UserListItemDto>> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new GetPostLikesQuery(postId), cancellationToken)).ToHttpResult());

        // ---- Saves (bookmarks) ----------------------------------------------------------------

        group.MapPost("/{postId:guid}/save", async (
                Guid postId,
                ICommandHandler<SavePostCommand, SaveStatusDto> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new SavePostCommand(postId), cancellationToken)).ToHttpResult());

        group.MapDelete("/{postId:guid}/save", async (
                Guid postId,
                ICommandHandler<UnsavePostCommand, SaveStatusDto> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new UnsavePostCommand(postId), cancellationToken)).ToHttpResult());

        // ---- Comments -------------------------------------------------------------------------

        group.MapGet("/{postId:guid}/comments", async (
                Guid postId,
                string? cursor,
                int? pageSize,
                IQueryHandler<GetCommentsQuery, CursorPage<CommentDto>> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new GetCommentsQuery(postId, cursor, pageSize), cancellationToken)).ToHttpResult());

        group.MapPost("/{postId:guid}/comments", async (
                Guid postId,
                AddCommentRequest request,
                ICommandHandler<AddCommentCommand, CommentDto> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new AddCommentCommand(postId, request.Text), cancellationToken))
                .ToHttpResult(comment => Results.Created($"/api/comments/{comment.Id}", comment)));

        app.MapDelete("/api/comments/{commentId:guid}", async (
                Guid commentId,
                ICommandHandler<DeleteCommentCommand, Unit> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new DeleteCommentCommand(commentId), cancellationToken)).ToHttpResult())
            .WithTags("Posts")
            .RequireAuthorization();
    }
}
