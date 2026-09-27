using FluentValidation;
using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Results;
using TheLife.Domain.Posts;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Features.Posts;

/// <summary>Only the caption can be edited; photos are fixed once posted, so likes and comments always refer to the same pictures.</summary>
public sealed record EditPostCommand(Guid PostId, string? Caption) : ICommand<PostDto>;

public sealed class EditPostValidator : AbstractValidator<EditPostCommand>
{
    public EditPostValidator()
    {
        RuleFor(x => x.Caption).MaximumLength(Post.CaptionMaxLength);
    }
}

public sealed class EditPostHandler(IAppDbContext db, ICurrentUser currentUser)
    : ICommandHandler<EditPostCommand, PostDto>
{
    public async Task<Result<PostDto>> Handle(EditPostCommand command, CancellationToken cancellationToken)
    {
        var me = currentUser.Id;

        var post = await db.Posts.SingleOrDefaultAsync(p => p.Id == command.PostId, cancellationToken);
        if (post is null) return PostErrors.NotFound;
        if (!post.IsAuthoredBy(me)) return PostErrors.NotAuthor;

        post.EditCaption(command.Caption);
        await db.SaveChangesAsync(cancellationToken);

        return await db.Posts
            .AsNoTracking()
            .Where(p => p.Id == post.Id)
            .Select(PostProjections.ToPostDto(me))
            .SingleAsync(cancellationToken);
    }
}
