using TheLife.Application.Common.Abstractions;
using TheLife.Domain.Notifications;
using TheLife.Domain.Posts;
using TheLife.Domain.Stories;
using TheLife.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace TheLife.Infrastructure.Persistence;

/// <summary>
/// Fills an EMPTY database with a few demo accounts, posts, follows, likes, comments and stories,
/// so the app has something to show on first run. Enabled with "Database:SeedDemoData" in appsettings.
/// Every demo account uses the password <see cref="DemoPassword"/>. Images come from picsum.photos.
/// </summary>
public sealed class DemoDataSeeder(AppDbContext db, IPasswordHasher passwordHasher, TimeProvider clock, ILogger<DemoDataSeeder> logger)
{
    public const string DemoPassword = "Password123!";

    private static readonly (string Username, string DisplayName, string Bio)[] DemoUsers =
    [
        ("demo", "Demo User", "Just trying out The Life 👋"),
        ("alice.travels", "Alice Martin", "✈️ 32 countries and counting"),
        ("bob.cooks", "Bob Nguyen", "Home cook. Pasta enthusiast 🍝"),
        ("carol.art", "Carol Smith", "Painter · Illustrator · Coffee"),
        ("dan.fit", "Dan Lee", "Lift heavy, run far 🏃"),
        ("emma.pets", "Emma Brown", "Two dogs, one cat, zero chill 🐶🐱"),
        ("frank.photo", "Frank Garcia", "Street & landscape photography 📷"),
    ];

    private static readonly string[] Captions =
    [
        "Golden hour never disappoints ✨",
        "Weekend mood",
        "Can't believe this view is real",
        "New recipe turned out better than expected!",
        "Throwback to one of my favorite days",
        "Small moments, big smiles",
        "Morning coffee and a good book ☕",
        "Exploring somewhere new today",
        "Nature is the best therapy 🌿",
        "Work in progress — what do you think?",
        "",
    ];

    private static readonly string[] CommentTexts =
    [
        "Wow, amazing! 😍", "Love this!", "Where is this?", "So good 🔥", "Beautiful shot",
        "Need to try this", "Goals!", "This made my day", "Incredible colors", "👏👏👏",
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Database already has users; skipping demo data");
            return;
        }

        var random = new Random(42); // fixed seed → the same demo data every time
        var now = clock.GetUtcNow().UtcDateTime;
        var passwordHash = passwordHasher.Hash(DemoPassword); // hash once, reuse for every demo account

        var users = DemoUsers
            .Select((u, index) =>
            {
                var user = User.Create(u.Username, $"{u.Username}@example.com", u.DisplayName, passwordHash, now.AddDays(-30 + index));
                user.UpdateProfile(u.DisplayName, u.Bio);
                user.ChangeAvatar($"https://picsum.photos/seed/avatar-{u.Username}/300/300");
                return user;
            })
            .ToList();
        db.Users.AddRange(users);

        var demo = users[0];
        var others = users.Skip(1).ToList();

        // Follows: the demo user follows three people; everybody else follows a few random people.
        var follows = new HashSet<(Guid, Guid)>();
        foreach (var followee in others.Take(3)) follows.Add((demo.Id, followee.Id));
        foreach (var user in others)
        {
            foreach (var followee in users.Where(u => u != user).OrderBy(_ => random.Next()).Take(3))
                follows.Add((user.Id, followee.Id));
        }
        db.Follows.AddRange(follows.Select(f => Follow.Create(f.Item1, f.Item2, now.AddDays(-20))));

        // Posts: 2 for the demo user, 4–6 for everybody else, spread over the last two weeks.
        var posts = new List<Post>();
        foreach (var user in users)
        {
            var postCount = user == demo ? 2 : random.Next(4, 7);
            for (var i = 0; i < postCount; i++)
            {
                var imageCount = random.Next(1, 4);
                var images = Enumerable.Range(0, imageCount)
                    .Select(n => new NewPostImage(
                        $"https://picsum.photos/seed/{user.Username}-{i}-{n}/1080/1080",
                        $"https://picsum.photos/seed/{user.Username}-{i}-{n}/320/320"))
                    .ToList();
                var createdAt = now.AddHours(-random.Next(1, 24 * 14));
                posts.Add(Post.Create(user.Id, Captions[random.Next(Captions.Length)], images, createdAt));
            }
        }
        db.Posts.AddRange(posts);

        // Likes and comments from random people, with notifications for the post author.
        foreach (var post in posts)
        {
            foreach (var liker in users.OrderBy(_ => random.Next()).Take(random.Next(0, users.Count)))
            {
                var likedAt = post.CreatedAt.AddMinutes(random.Next(1, 600));
                db.Likes.Add(Like.Create(post.Id, liker.Id, likedAt));
                if (liker.Id != post.AuthorId)
                    db.Notifications.Add(Notification.ForLike(post.AuthorId, liker.Id, post.Id, likedAt));
            }

            foreach (var commenter in users.OrderBy(_ => random.Next()).Take(random.Next(0, 4)))
            {
                var commentedAt = post.CreatedAt.AddMinutes(random.Next(1, 600));
                var comment = Comment.Create(post.Id, commenter.Id, CommentTexts[random.Next(CommentTexts.Length)], commentedAt);
                db.Comments.Add(comment);
                if (commenter.Id != post.AuthorId)
                    db.Notifications.Add(Notification.ForComment(post.AuthorId, commenter.Id, post.Id, comment.Text, commentedAt));
            }
        }

        // A few active stories.
        foreach (var user in others.Take(4))
        {
            for (var i = 0; i < random.Next(1, 4); i++)
            {
                var imageUrl = $"https://picsum.photos/seed/story-{user.Username}-{i}/1080/1920";
                db.Stories.Add(Story.Create(user.Id, imageUrl, now.AddHours(-random.Next(1, 20))));
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Users} demo users and {Posts} posts. Log in as 'demo' / '{Password}'",
            users.Count, posts.Count, DemoPassword);
    }
}
