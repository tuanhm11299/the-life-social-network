using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using TheLife.Api.Common;
using TheLife.Application.Common.Abstractions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi;

namespace TheLife.Api;

/// <summary>Web-specific services: current user, error handling, CORS, rate limiting, JSON and OpenAPI.</summary>
internal static class ApiServices
{
    public const string AuthRateLimitPolicy = "auth";

    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // Enums are sent as text ("Like") instead of numbers (1), which is easier to read in the frontend.
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        // Only needed when the frontend calls the API from another origin. With the Nuxt dev proxy
        // (the default setup) the browser sees a single origin and CORS is not involved.
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(options => options.AddDefaultPolicy(policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

        // Slows down password guessing: limited login/register attempts per IP address per minute.
        var authRequestsPerMinute = configuration.GetValue("RateLimiting:AuthRequestsPerMinute", 20);
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(AuthRateLimitPolicy, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = authRequestsPerMinute,
                    Window = TimeSpan.FromMinutes(1),
                }));
        });

        // Behind a reverse proxy, trust X-Forwarded-For / X-Forwarded-Proto for the client IP and https.
        services.Configure<ForwardedHeadersOptions>(options =>
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

        services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
        {
            // Adds the "Authorize" button to the API docs page (paste an access token there).
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
            };
            document.Security = [new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] }];
            return Task.CompletedTask;
        }));

        return services;
    }
}
