using System.Net;
using System.Net.Http.Json;

namespace TheLife.Tests.Api;

/// <summary>Following people, profiles, search and stories.</summary>
[Collection(ApiCollection.Name)]
public class SocialTests(ApiFactory factory)
{
    [Fact]
    public async Task Following_updates_counters_lists_and_notifications()
    {
        var alice = await factory.CreateUserAsync("alice");
        var bob = await factory.CreateUserAsync("bob");

        var follow = await alice.Client.PostAsync($"/api/users/{bob.Username}/follow", null);
        Assert.Equal(new FollowStatusDto(true, 1), await follow.Content.ReadFromJsonAsync<FollowStatusDto>());

        var bobProfile = await alice.Client.GetFromJsonAsync<ProfileDto>($"/api/users/{bob.Username}");
        Assert.True(bobProfile!.IsFollowedByMe);
        Assert.Equal(1, bobProfile.FollowerCount);

        var followers = await bob.Client.GetFromJsonAsync<PageDto<UserDto>>($"/api/users/{bob.Username}/followers");
        Assert.Single(followers!.Items, u => u.Id == alice.Id);

        var notifications = await bob.Client.GetFromJsonAsync<PageDto<NotificationDto>>("/api/notifications");
        Assert.Single(notifications!.Items, n => n.Type == "Follow" && n.Actor.Id == alice.Id);

        var unfollow = await alice.Client.DeleteAsync($"/api/users/{bob.Username}/follow");
        Assert.Equal(new FollowStatusDto(false, 0), await unfollow.Content.ReadFromJsonAsync<FollowStatusDto>());
    }

    [Fact]
    public async Task Users_cannot_follow_themselves()
    {
        var user = await factory.CreateUserAsync();

        var response = await user.Client.PostAsync($"/api/users/{user.Username}/follow", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_profile_returns_404()
    {
        var user = await factory.CreateUserAsync();

        var response = await user.Client.GetAsync("/api/users/does.not.exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Search_finds_people_by_part_of_their_username()
    {
        var searcher = await factory.CreateUserAsync();
        var target = await factory.CreateUserAsync("findme");

        var results = await searcher.Client.GetFromJsonAsync<List<UserDto>>($"/api/users/search?q=@{target.Username[..10]}");

        Assert.Contains(results!, u => u.Id == target.Id);
    }

    [Fact]
    public async Task Stories_show_up_for_followers_and_only_the_author_sees_who_viewed_them()
    {
        var author = await factory.CreateUserAsync("storyteller");
        var follower = await factory.CreateUserAsync("watcher");
        await follower.Client.PostAsync($"/api/users/{author.Username}/follow", null);

        var upload = new MultipartFormDataContent { { TestImages.PngFile(), "image", "story.png" } };
        var created = await author.Client.PostAsync("/api/stories", upload);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var story = await created.Content.ReadFromJsonAsync<StoryDto>();

        var tray = await follower.Client.GetFromJsonAsync<List<StoryTrayItemDto>>("/api/stories");
        var item = Assert.Single(tray!, i => i.User.Id == author.Id);
        Assert.True(item.HasUnseen);

        Assert.Equal(HttpStatusCode.NoContent, (await follower.Client.PostAsync($"/api/stories/{story!.Id}/view", null)).StatusCode);

        var trayAfterViewing = await follower.Client.GetFromJsonAsync<List<StoryTrayItemDto>>("/api/stories");
        Assert.False(trayAfterViewing!.Single(i => i.User.Id == author.Id).HasUnseen);

        var viewers = await author.Client.GetFromJsonAsync<List<UserDto>>($"/api/stories/{story.Id}/viewers");
        Assert.Single(viewers!, v => v.Id == follower.Id);

        Assert.Equal(HttpStatusCode.Forbidden, (await follower.Client.GetAsync($"/api/stories/{story.Id}/viewers")).StatusCode);
    }
}
