using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Features.Auth;

public sealed record GetCurrentUserQuery : IQuery<CurrentUserDto>;

public sealed class GetCurrentUserHandler(IAppDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetCurrentUserQuery, CurrentUserDto>
{
    public async Task<Result<CurrentUserDto>> Handle(GetCurrentUserQuery query, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == currentUser.Id, cancellationToken);

        return user is null ? AuthErrors.UserNotFound : CurrentUserDto.From(user);
    }
}
