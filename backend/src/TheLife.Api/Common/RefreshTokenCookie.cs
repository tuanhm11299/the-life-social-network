namespace TheLife.Api.Common;

/// <summary>
/// The refresh token lives in an http-only cookie: JavaScript cannot read it, so an XSS bug cannot steal it.
/// The cookie is only sent to /api/auth/* and never to other sites (SameSite=Strict).
/// </summary>
internal static class RefreshTokenCookie
{
    private const string Name = "thelife_refresh";
    private const string Path = "/api/auth";

    public static void Write(HttpContext httpContext, string refreshToken, DateTime expiresAt) =>
        httpContext.Response.Cookies.Append(Name, refreshToken, CreateOptions(httpContext, expiresAt));

    public static string? Read(HttpContext httpContext) => httpContext.Request.Cookies[Name];

    public static void Delete(HttpContext httpContext) =>
        httpContext.Response.Cookies.Delete(Name, CreateOptions(httpContext, expiresAt: null));

    private static CookieOptions CreateOptions(HttpContext httpContext, DateTime? expiresAt) => new()
    {
        HttpOnly = true,
        Secure = httpContext.Request.IsHttps,
        SameSite = SameSiteMode.Strict,
        Path = Path,
        Expires = expiresAt,
        IsEssential = true,
    };
}
