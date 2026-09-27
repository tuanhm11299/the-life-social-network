using TheLife.Api.Common;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Pagination;
using TheLife.Application.Common.Results;
using TheLife.Application.Features.Notifications;

namespace TheLife.Api.Endpoints;

internal static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications").WithTags("Notifications").RequireAuthorization();

        group.MapGet("/", async (
                string? cursor,
                int? pageSize,
                IQueryHandler<GetNotificationsQuery, CursorPage<NotificationDto>> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new GetNotificationsQuery(cursor, pageSize), cancellationToken)).ToHttpResult());

        group.MapGet("/unread-count", async (
                IQueryHandler<GetUnreadNotificationCountQuery, UnreadCountDto> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new GetUnreadNotificationCountQuery(), cancellationToken)).ToHttpResult());

        group.MapPost("/mark-read", async (
                ICommandHandler<MarkNotificationsReadCommand, Unit> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new MarkNotificationsReadCommand(), cancellationToken)).ToHttpResult());
    }
}
