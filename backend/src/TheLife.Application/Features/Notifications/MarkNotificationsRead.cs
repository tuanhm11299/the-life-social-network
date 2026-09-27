using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Features.Notifications;

public sealed record MarkNotificationsReadCommand : ICommand<Unit>;

/// <summary>Marks all my notifications as read (called when the notifications page is opened).</summary>
public sealed class MarkNotificationsReadHandler(IAppDbContext db, ICurrentUser currentUser)
    : ICommandHandler<MarkNotificationsReadCommand, Unit>
{
    public async Task<Result<Unit>> Handle(MarkNotificationsReadCommand command, CancellationToken cancellationToken)
    {
        // A single UPDATE statement; no need to load the rows into memory first.
        await db.Notifications
            .Where(n => n.RecipientId == currentUser.Id && !n.IsRead)
            .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true), cancellationToken);

        return Unit.Value;
    }
}
