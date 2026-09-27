using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Features.Stories;

public sealed record DeleteStoryCommand(Guid StoryId) : ICommand<Unit>;

public sealed class DeleteStoryHandler(IAppDbContext db, ICurrentUser currentUser, IFileStorage fileStorage)
    : ICommandHandler<DeleteStoryCommand, Unit>
{
    public async Task<Result<Unit>> Handle(DeleteStoryCommand command, CancellationToken cancellationToken)
    {
        var story = await db.Stories.SingleOrDefaultAsync(s => s.Id == command.StoryId, cancellationToken);

        if (story is null) return StoryErrors.NotFound;
        if (story.AuthorId != currentUser.Id) return StoryErrors.NotAuthor;

        db.Stories.Remove(story);
        await db.SaveChangesAsync(cancellationToken);
        await fileStorage.DeleteAsync(story.ImageUrl, cancellationToken);

        return Unit.Value;
    }
}
