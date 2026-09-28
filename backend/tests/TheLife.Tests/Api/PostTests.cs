using System.Net;
using System.Net.Http.Json;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace TheLife.Tests.Api;

[Collection(ApiCollection.Name)]
public class PostTests(ApiFactory factory)
{
    private static async Task<PostDto> CreatePostAsync(TestUser author, string caption = "Hello", int imageCount = 1)
    {
        var response = await author.Client.PostAsync("/api/posts", TestImages.PostForm(caption, imageCount));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PostDto>())!;
    }

    [Fact]
    public async Task Created_post_is_stored_with_its_images_and_can_be_downloaded()
    {
        var author = await factory.CreateUserAsync();

        var post = await CreatePostAsync(author, "Sunset #beach", imageCount: 3);

        Assert.Equal("Sunset #beach", post.Caption);
        Assert.Equal(3, post.ImageUrls.Count);
        var image = await author.Client.GetAsync(post.ImageUrls[0]);
        Assert.Equal(HttpStatusCode.OK, image.StatusCode);
        Assert.Equal("image/png", image.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Files_that_are_not_images_are_rejected()
    {
        var author = await factory.CreateUserAsync();
        var form = new MultipartFormDataContent
        {
            { TestImages.File(Encoding.UTF8.GetBytes("MZ fake exe"), "image/png"), "images", "evil.png" },
        };

        var response = await author.Client.PostAsync("/api/posts", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Uploaded_photos_are_stored_without_their_gps_location()
    {
        var author = await factory.CreateUserAsync();
        var form = new MultipartFormDataContent
        {
            { TestImages.File(TestImages.PhotoWithLocation(JpegFormat.Instance), "image/jpeg"), "images", "phone.jpg" },
        };

        var response = await author.Client.PostAsync("/api/posts", form);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var post = (await response.Content.ReadFromJsonAsync<PostDto>())!;
        var stored = await author.Client.GetByteArrayAsync(post.ImageUrls[0]);
        Assert.Null(Image.Identify(stored).Metadata.ExifProfile);

        // Also check the raw bytes, without trusting the image library: every EXIF block starts with "Exif\0\0".
        Assert.Equal(-1, stored.AsSpan().IndexOf("Exif\0\0"u8));
    }

    [Fact]
    public async Task Broken_images_are_rejected_with_400()
    {
        var author = await factory.CreateUserAsync();
        byte[] broken = [0xFF, 0xD8, 0xFF, 0xE0, .. Enumerable.Repeat((byte)0x42, 200)];
        var form = new MultipartFormDataContent { { TestImages.File(broken, "image/jpeg"), "images", "broken.jpg" } };

        var response = await author.Client.PostAsync("/api/posts", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Feed_shows_posts_from_followed_people_and_explore_shows_the_others()
    {
        var reader = await factory.CreateUserAsync("reader");
        var followed = await factory.CreateUserAsync("followed");
        var stranger = await factory.CreateUserAsync("stranger");
        var followedPost = await CreatePostAsync(followed);
        var strangerPost = await CreatePostAsync(stranger);

        (await reader.Client.PostAsync($"/api/users/{followed.Username}/follow", null)).EnsureSuccessStatusCode();

        var feed = await reader.Client.GetFromJsonAsync<PageDto<PostDto>>("/api/posts/feed?pageSize=50");
        Assert.Contains(feed!.Items, p => p.Id == followedPost.Id);
        Assert.DoesNotContain(feed.Items, p => p.Id == strangerPost.Id);

        var explore = await reader.Client.GetFromJsonAsync<PageDto<PostDto>>("/api/posts/explore?pageSize=50");
        Assert.Contains(explore!.Items, p => p.Id == strangerPost.Id);
        Assert.DoesNotContain(explore.Items, p => p.Id == followedPost.Id);
    }

    [Fact]
    public async Task Feed_pages_do_not_overlap()
    {
        var author = await factory.CreateUserAsync();
        for (var i = 0; i < 5; i++) await CreatePostAsync(author, $"post {i}");

        var seen = new List<Guid>();
        string? cursor = null;
        do
        {
            var page = await author.Client.GetFromJsonAsync<PageDto<PostDto>>($"/api/posts/feed?pageSize=2&cursor={cursor}");
            seen.AddRange(page!.Items.Select(p => p.Id));
            cursor = page.NextCursor;
        }
        while (cursor is not null);

        Assert.Equal(5, seen.Count);
        Assert.Equal(5, seen.Distinct().Count());
    }

    [Fact]
    public async Task Liking_twice_counts_once_and_notifies_the_author_once()
    {
        var author = await factory.CreateUserAsync();
        var fan = await factory.CreateUserAsync();
        var post = await CreatePostAsync(author);

        await fan.Client.PostAsync($"/api/posts/{post.Id}/like", null);
        var second = await fan.Client.PostAsync($"/api/posts/{post.Id}/like", null);
        var status = await second.Content.ReadFromJsonAsync<LikeStatusDto>();
        Assert.Equal(new LikeStatusDto(true, 1), status);

        var notifications = await author.Client.GetFromJsonAsync<PageDto<NotificationDto>>("/api/notifications");
        Assert.Single(notifications!.Items, n => n.Type == "Like" && n.Actor.Id == fan.Id && n.PostId == post.Id);

        var unlike = await fan.Client.DeleteAsync($"/api/posts/{post.Id}/like");
        Assert.Equal(new LikeStatusDto(false, 0), await unlike.Content.ReadFromJsonAsync<LikeStatusDto>());
    }

    [Fact]
    public async Task Comments_can_be_deleted_by_their_author_or_the_post_owner_but_nobody_else()
    {
        var owner = await factory.CreateUserAsync();
        var commenter = await factory.CreateUserAsync();
        var stranger = await factory.CreateUserAsync();
        var post = await CreatePostAsync(owner);

        var created = await commenter.Client.PostAsJsonAsync($"/api/posts/{post.Id}/comments", new { text = "Love it @someone" });
        var comment = await created.Content.ReadFromJsonAsync<CommentDto>();
        Assert.True(comment!.CanDelete);

        var byStranger = await stranger.Client.DeleteAsync($"/api/comments/{comment.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, byStranger.StatusCode);

        var byOwner = await owner.Client.DeleteAsync($"/api/comments/{comment.Id}");
        Assert.Equal(HttpStatusCode.NoContent, byOwner.StatusCode);

        var comments = await owner.Client.GetFromJsonAsync<PageDto<CommentDto>>($"/api/posts/{post.Id}/comments");
        Assert.Empty(comments!.Items);
    }

    [Fact]
    public async Task Only_the_author_can_edit_or_delete_a_post()
    {
        var author = await factory.CreateUserAsync();
        var other = await factory.CreateUserAsync();
        var post = await CreatePostAsync(author);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await other.Client.PatchAsJsonAsync($"/api/posts/{post.Id}", new { caption = "hacked" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.Client.DeleteAsync($"/api/posts/{post.Id}")).StatusCode);

        var edited = await author.Client.PatchAsJsonAsync($"/api/posts/{post.Id}", new { caption = "Edited" });
        Assert.Equal("Edited", (await edited.Content.ReadFromJsonAsync<PostDto>())!.Caption);

        Assert.Equal(HttpStatusCode.NoContent, (await author.Client.DeleteAsync($"/api/posts/{post.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await author.Client.GetAsync($"/api/posts/{post.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await author.Client.GetAsync(post.ImageUrls[0])).StatusCode);
    }

    [Fact]
    public async Task Saved_posts_appear_in_the_saved_list()
    {
        var author = await factory.CreateUserAsync();
        var reader = await factory.CreateUserAsync();
        var post = await CreatePostAsync(author);

        await reader.Client.PostAsync($"/api/posts/{post.Id}/save", null);
        var saved = await reader.Client.GetFromJsonAsync<PageDto<PostDto>>("/api/posts/saved");
        Assert.Single(saved!.Items, p => p.Id == post.Id && p.IsSavedByMe);

        await reader.Client.DeleteAsync($"/api/posts/{post.Id}/save");
        var afterUnsave = await reader.Client.GetFromJsonAsync<PageDto<PostDto>>("/api/posts/saved");
        Assert.Empty(afterUnsave!.Items);
    }
}
