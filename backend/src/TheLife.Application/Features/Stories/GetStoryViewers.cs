using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Results;
using TheLife.Application.Features.Users;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Features.Stories;

public sealed record GetStoryViewersQuery(Guid StoryId) : IQuery<IReadOnlyList<UserListItemDto>>;

/// <summary>"Seen by": who viewed my story. Only the story author may see this list.</summary>
public sealed class GetStoryViewersHandler(IAppDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetStoryViewersQuery, IReadOnlyList<UserListItemDto>>
{
    public async Task<Result<IReadOnlyList<UserListItemDto>>> Handle(GetStoryViewersQuery query, CancellationToken cancellationToken)
    {
        var me = currentUser.Id;

        var authorId = await db.Stories
            .Where(s => s.Id == query.StoryId)
            .Select(s => (Guid?)s.AuthorId)
            .SingleOrDefaultAsync(cancellationToken);

        if (authorId is null) return StoryErrors.NotFound;
        if (authorId != me) return StoryErrors.NotAuthor;

        var viewers = await db.StoryViews
            .AsNoTracking()
            .Where(v => v.StoryId == query.StoryId)
            .OrderByDescending(v => v.ViewedAt)
            .Select(v => v.Viewer)
            .Select(UserProjections.ToListItem(me))
            .ToListAsync(cancellationToken);

        return viewers;
    }
}
