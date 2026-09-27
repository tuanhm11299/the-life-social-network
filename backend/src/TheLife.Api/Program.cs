using TheLife.Api;
using TheLife.Api.Endpoints;
using TheLife.Application;
using TheLife.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Each layer registers its own services (see the DependencyInjection/ApiServices classes).
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApi(builder.Configuration);

var app = builder.Build();

// The order of middleware matters: errors first, then routing-related pieces, then endpoints.
app.UseExceptionHandler();
app.UseForwardedHeaders();
app.UseCors();
app.UseUploadedFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();              // GET /openapi/v1.json
    app.MapScalarApiReference();   // interactive API docs at /scalar
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).WithTags("Health");
app.MapAuthEndpoints();
app.MapAccountEndpoints();
app.MapUserEndpoints();
app.MapPostEndpoints();
app.MapStoryEndpoints();
app.MapNotificationEndpoints();

await app.InitializeDatabaseAsync();
await app.RunAsync();
