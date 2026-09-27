using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Pagination;
using TheLife.Application.Common.Results;
using TheLife.Application.Features.Users;
using TheLife.Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Features.Notifications;

/// <param name="PostImageUrl">Thumbnail of the liked/commented post (null for follow notifications).</param>
/// <param name="IsFollowingActor">Whether I already follow the actor (to show a "Follow back" button).</param>
public sealed record NotificationDto(
    Guid Id,
    NotificationType Type,
    UserSummaryDto Actor,
    Guid? PostId,
    string? PostImageUrl,
    string? CommentText,
    DateTime CreatedAt,
    bool IsRead,
    bool IsFollowingActor);

public sealed record GetNotificationsQuery(string? Cursor, int? PageSize) : IQuery<CursorPage<NotificationDto>>;

/// <summary>My notifications ("jane liked your post"), newest first.</summary>
public sealed class GetNotificationsHandler(IAppDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetNotificationsQuery, CursorPage<NotificationDto>>
{
    public async Task<Result<CursorPage<NotificationDto>>> Handle(GetNotificationsQuery query, CancellationToken cancellationToken)
    {
        var me = currentUser.Id;
        var pageSize = Cursor.ClampPageSize(query.PageSize);

        var notifications = db.Notifications.AsNoTracking().Where(n => n.RecipientId == me);

        var before = Cursor.ToDate(query.Cursor);
        if (before is not null) notifications = notifications.Where(n => n.CreatedAt < before);

        var items = await notifications
            .OrderByDescending(n => n.CreatedAt)
            .Take(pageSize + 1)
            .Select(n => new NotificationDto(
                n.Id,
                n.Type,
                new UserSummaryDto(n.Actor.Id, n.Actor.Username, n.Actor.DisplayName, n.Actor.AvatarUrl),
                n.PostId,
                n.Post == null ? null : n.Post.Images.OrderBy(i => i.Position).Select(i => i.Url).FirstOrDefault(),
                n.CommentText,
                n.CreatedAt,
                n.IsRead,
                n.Actor.Followers.Any(f => f.FollowerId == me)))
            .ToListAsync(cancellationToken);

        return CursorPage<NotificationDto>.FromOverfetched(items, pageSize, n => Cursor.FromDate(n.CreatedAt));
    }
}
