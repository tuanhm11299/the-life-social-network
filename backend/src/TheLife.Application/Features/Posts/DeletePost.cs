using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Features.Posts;

public sealed record DeletePostCommand(Guid PostId) : ICommand<Unit>;

/// <summary>
/// Deletes a post. Its images, likes, comments, saves and notifications are removed by the
/// database (cascade delete), then the image files are removed from storage.
/// </summary>
public sealed class DeletePostHandler(IAppDbContext db, ICurrentUser currentUser, IFileStorage fileStorage)
    : ICommandHandler<DeletePostCommand, Unit>
{
    public async Task<Result<Unit>> Handle(DeletePostCommand command, CancellationToken cancellationToken)
    {
        var post = await db.Posts
            .Include(p => p.Images)
            .SingleOrDefaultAsync(p => p.Id == command.PostId, cancellationToken);

        if (post is null) return PostErrors.NotFound;
        if (!post.IsAuthoredBy(currentUser.Id)) return PostErrors.NotAuthor;

        var imageUrls = post.Images.Select(image => image.Url).ToList();

        db.Posts.Remove(post);
        await db.SaveChangesAsync(cancellationToken);

        foreach (var url in imageUrls) await fileStorage.DeleteAsync(url, cancellationToken);

        return Unit.Value;
    }
}
