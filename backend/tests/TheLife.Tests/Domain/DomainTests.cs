using TheLife.Application.Common.Pagination;
using TheLife.Domain.Common;
using TheLife.Domain.Notifications;
using TheLife.Domain.Posts;
using TheLife.Domain.Stories;
using TheLife.Domain.Users;

namespace TheLife.Tests.Domain;

// Fast unit tests for the business rules inside the domain entities. No database needed.

public class PostTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_keeps_images_in_the_given_order()
    {
        var post = Post.Create(Guid.NewGuid(), "  Hello  ", ["/a.jpg", "/b.jpg", "/c.jpg"], Now);

        Assert.Equal("Hello", post.Caption);
        Assert.Equal(["/a.jpg", "/b.jpg", "/c.jpg"], post.Images.OrderBy(i => i.Position).Select(i => i.Url));
        Assert.All(post.Images, image => Assert.Equal(post.Id, image.PostId));
    }

    [Fact]
    public void Create_requires_at_least_one_image()
    {
        Assert.Throws<DomainException>(() => Post.Create(Guid.NewGuid(), "caption", [], Now));
    }

    [Fact]
    public void Create_allows_at_most_ten_images()
    {
        var images = Enumerable.Range(0, Post.MaxImages + 1).Select(i => $"/{i}.jpg").ToList();

        Assert.Throws<DomainException>(() => Post.Create(Guid.NewGuid(), null, images, Now));
    }

    [Fact]
    public void EditCaption_rejects_captions_that_are_too_long()
    {
        var post = Post.Create(Guid.NewGuid(), null, ["/a.jpg"], Now);

        Assert.Throws<DomainException>(() => post.EditCaption(new string('x', Post.CaptionMaxLength + 1)));
    }
}

public class CommentTests
{
    [Fact]
    public void Comment_can_be_deleted_by_its_author_or_the_post_author_only()
    {
        var commentAuthor = Guid.NewGuid();
        var postAuthor = Guid.NewGuid();
        var comment = Comment.Create(Guid.NewGuid(), commentAuthor, "Nice!", DateTime.UtcNow);

        Assert.True(comment.CanBeDeletedBy(commentAuthor, postAuthor));
        Assert.True(comment.CanBeDeletedBy(postAuthor, postAuthor));
        Assert.False(comment.CanBeDeletedBy(Guid.NewGuid(), postAuthor));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Comment_cannot_be_empty(string text)
    {
        Assert.Throws<DomainException>(() => Comment.Create(Guid.NewGuid(), Guid.NewGuid(), text, DateTime.UtcNow));
    }
}

public class UserTests
{
    [Fact]
    public void Username_and_email_are_stored_lower_case()
    {
        var user = User.Create(" Jane.Doe ", "JANE@Example.com", "", "hash", DateTime.UtcNow);

        Assert.Equal("jane.doe", user.Username);
        Assert.Equal("jane@example.com", user.Email);
        Assert.Equal("Jane.Doe", user.DisplayName); // falls back to the username as typed
    }

    [Fact]
    public void Users_cannot_follow_themselves()
    {
        var id = Guid.NewGuid();

        Assert.Throws<DomainException>(() => Follow.Create(id, id, DateTime.UtcNow));
    }

    [Fact]
    public void Users_are_not_notified_about_their_own_actions()
    {
        var id = Guid.NewGuid();

        Assert.Throws<DomainException>(() => Notification.ForFollow(id, id, DateTime.UtcNow));
    }
}

public class RefreshTokenTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Replaced_token_keeps_working_during_the_grace_period_only()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", Now, TimeSpan.FromDays(30));
        token.MarkReplaced(Now);

        Assert.True(token.IsActive(Now.AddSeconds(5)));
        Assert.False(token.IsActive(Now + RefreshToken.RotationGracePeriod));
    }

    [Fact]
    public void Revoked_token_stops_working_immediately()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", Now, TimeSpan.FromDays(30));
        token.Revoke(Now);

        Assert.False(token.IsActive(Now));
    }

    [Fact]
    public void Expired_token_does_not_work()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", Now, TimeSpan.FromDays(1));

        Assert.False(token.IsActive(Now.AddDays(2)));
    }
}

public class StoryTests
{
    [Fact]
    public void Story_expires_after_24_hours()
    {
        var now = DateTime.UtcNow;
        var story = Story.Create(Guid.NewGuid(), "/s.jpg", now);

        Assert.True(story.IsActive(now.AddHours(23)));
        Assert.False(story.IsActive(now.AddHours(24)));
    }
}

public class CursorTests
{
    [Fact]
    public void Date_cursor_round_trips()
    {
        var date = new DateTime(2026, 5, 6, 7, 8, 9, DateTimeKind.Utc).AddTicks(1234560);

        Assert.Equal(date, Cursor.ToDate(Cursor.FromDate(date)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-number")]
    [InlineData("-5")]
    public void Invalid_cursor_means_start_from_the_top(string? cursor)
    {
        Assert.Null(Cursor.ToDate(cursor));
        Assert.Equal(0, Cursor.ToOffset(cursor));
    }

    [Fact]
    public void Page_is_built_from_an_overfetched_list()
    {
        var page = CursorPage<int>.FromOverfetched([1, 2, 3], pageSize: 2, cursorOf: i => i.ToString());

        Assert.Equal([1, 2], page.Items);
        Assert.Equal("2", page.NextCursor);
    }
}
