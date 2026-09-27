using System.Net;
using System.Net.Http.Json;

namespace TheLife.Tests.Api;

[Collection(ApiCollection.Name)]
public class AuthTests(ApiFactory factory)
{
    [Fact]
    public async Task Registered_user_can_read_their_own_account()
    {
        var user = await factory.CreateUserAsync();

        var me = await user.Client.GetFromJsonAsync<UserDto>("/api/auth/me");

        Assert.Equal(user.Id, me!.Id);
        Assert.Equal(user.Username, me.Username);
    }

    [Fact]
    public async Task Username_must_be_unique()
    {
        var existing = await factory.CreateUserAsync();

        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register",
            new { username = existing.Username.ToUpperInvariant(), email = "other@test.local", password = TestUser.Password });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDto>();
        Assert.Equal("Auth.UsernameTaken", problem!.Code);
    }

    [Fact]
    public async Task Invalid_registration_returns_errors_per_field()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register",
            new { username = "no spaces!", email = "not-an-email", password = "short" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDto>();
        Assert.Contains("username", problem!.Errors!.Keys);
        Assert.Contains("email", problem.Errors.Keys);
        Assert.Contains("password", problem.Errors.Keys);
    }

    [Fact]
    public async Task Login_with_wrong_password_is_rejected()
    {
        var user = await factory.CreateUserAsync();

        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { login = user.Username, password = "WrongPassword1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_cookie_gives_a_new_access_token_until_logout()
    {
        var user = await factory.CreateUserAsync();
        var browser = factory.CreateClient(); // keeps cookies, like a browser

        var login = await browser.PostAsJsonAsync("/api/auth/login", new { login = user.Username, password = TestUser.Password });
        login.EnsureSuccessStatusCode();

        var refresh = await browser.PostAsync("/api/auth/refresh", null);
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);

        var logout = await browser.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var refreshAfterLogout = await browser.PostAsync("/api/auth/refresh", null);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshAfterLogout.StatusCode);
    }

    [Fact]
    public async Task Private_endpoints_require_a_token()
    {
        var response = await factory.CreateClient().GetAsync("/api/posts/feed");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Changing_password_requires_the_current_one()
    {
        var user = await factory.CreateUserAsync();

        var wrong = await user.Client.PostAsJsonAsync("/api/account/change-password",
            new { currentPassword = "Nope12345", newPassword = "NewPassword456" });
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);

        var right = await user.Client.PostAsJsonAsync("/api/account/change-password",
            new { currentPassword = TestUser.Password, newPassword = "NewPassword456" });
        Assert.Equal(HttpStatusCode.NoContent, right.StatusCode);

        var loginWithNew = await factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { login = user.Username, password = "NewPassword456" });
        Assert.Equal(HttpStatusCode.OK, loginWithNew.StatusCode);
    }
}
