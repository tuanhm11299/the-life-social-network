using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Features.Users;

public sealed record SearchUsersQuery(string? Term) : IQuery<IReadOnlyList<UserListItemDto>>;

/// <summary>Finds people whose username or display name contains the search term.</summary>
public sealed class SearchUsersHandler(IAppDbContext db, ICurrentUser currentUser)
    : IQueryHandler<SearchUsersQuery, IReadOnlyList<UserListItemDto>>
{
    private const int MaxResults = 20;

    public async Task<Result<IReadOnlyList<UserListItemDto>>> Handle(SearchUsersQuery query, CancellationToken cancellationToken)
    {
        // People often type "@jane"; the @ is not part of the username.
        var term = (query.Term ?? "").Trim().TrimStart('@').ToLowerInvariant();
        if (term.Length == 0) return Array.Empty<UserListItemDto>();

        var users = await db.Users
            .AsNoTracking()
            .Where(u => u.Username.Contains(term) || u.DisplayName.ToLower().Contains(term))
            // Usernames that start with the term first, then the most followed people.
            .OrderByDescending(u => u.Username.StartsWith(term))
            .ThenByDescending(u => u.Followers.Count)
            .ThenBy(u => u.Username)
            .Take(MaxResults)
            .Select(UserProjections.ToListItem(currentUser.Id))
            .ToListAsync(cancellationToken);

        return users;
    }
}
