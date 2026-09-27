using FluentValidation;
using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Results;
using TheLife.Application.Features.Posts;
using TheLife.Domain.Notifications;
using TheLife.Domain.Posts;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Features.Comments;

public sealed record AddCommentCommand(Guid PostId, string Text) : ICommand<CommentDto>;

public sealed class AddCommentValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Write something first.")
            .MaximumLength(Comment.TextMaxLength);
    }
}

/// <summary>Adds a comment to a post and notifies the post author.</summary>
public sealed class AddCommentHandler(IAppDbContext db, ICurrentUser currentUser, TimeProvider clock)
    : ICommandHandler<AddCommentCommand, CommentDto>
{
    public async Task<Result<CommentDto>> Handle(AddCommentCommand command, CancellationToken cancellationToken)
    {
        var me = currentUser.Id;

        var postAuthorId = await db.Posts
            .Where(p => p.Id == command.PostId)
            .Select(p => (Guid?)p.AuthorId)
            .SingleOrDefaultAsync(cancellationToken);

        if (postAuthorId is null) return PostErrors.NotFound;

        var now = clock.GetUtcNow().UtcDateTime;
        var comment = Comment.Create(command.PostId, me, command.Text, now);
        db.Comments.Add(comment);

        if (postAuthorId != me)
            db.Notifications.Add(Notification.ForComment(postAuthorId.Value, me, command.PostId, comment.Text, now));

        await db.SaveChangesAsync(cancellationToken);

        return await db.Comments
            .AsNoTracking()
            .Where(c => c.Id == comment.Id)
            .Select(CommentProjections.ToCommentDto(me))
            .SingleAsync(cancellationToken);
    }
}
