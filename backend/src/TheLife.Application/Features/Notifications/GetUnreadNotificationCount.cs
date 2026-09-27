using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Features.Notifications;

public sealed record UnreadCountDto(int Count);

public sealed record GetUnreadNotificationCountQuery : IQuery<UnreadCountDto>;

/// <summary>Number for the red badge on the heart icon.</summary>
public sealed class GetUnreadNotificationCountHandler(IAppDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetUnreadNotificationCountQuery, UnreadCountDto>
{
    public async Task<Result<UnreadCountDto>> Handle(GetUnreadNotificationCountQuery query, CancellationToken cancellationToken)
    {
        var count = await db.Notifications.CountAsync(n => n.RecipientId == currentUser.Id && !n.IsRead, cancellationToken);
        return new UnreadCountDto(count);
    }
}
