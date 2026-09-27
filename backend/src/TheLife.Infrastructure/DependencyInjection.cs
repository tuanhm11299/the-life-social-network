using TheLife.Application.Common.Abstractions;
using TheLife.Infrastructure.Authentication;
using TheLife.Infrastructure.BackgroundJobs;
using TheLife.Infrastructure.Persistence;
using TheLife.Infrastructure.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace TheLife.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers the database, authentication, file storage and background jobs.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Database' is missing from configuration.");

        services.AddDbContext<AppDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddScoped<DemoDataSeeder>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddJwtAuthentication();

        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.AddSingleton<LocalFileStorage>();
        services.AddSingleton<IFileStorage>(provider => provider.GetRequiredService<LocalFileStorage>());

        services.AddSingleton(TimeProvider.System);
        services.AddHostedService<ExpiredStoryCleanupJob>();

        return services;
    }

    /// <summary>Applies pending EF Core migrations and, if enabled, adds demo data to an empty database.</summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DependencyInjection));

        if (app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", false))
        {
            logger.LogInformation("Applying database migrations");
            await db.Database.MigrateAsync();
        }

        if (app.Configuration.GetValue("Database:SeedDemoData", false))
        {
            await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync();
        }
    }

    /// <summary>Serves uploaded images, e.g. GET /uploads/posts/abc.jpg.</summary>
    public static WebApplication UseUploadedFiles(this WebApplication app)
    {
        var storage = app.Services.GetRequiredService<LocalFileStorage>();
        var publicBasePath = app.Configuration.GetValue("Storage:PublicBasePath", "/uploads")!;

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(storage.RootDirectory),
            RequestPath = publicBasePath,
            OnPrepareResponse = context =>
            {
                // File names are unique and never change, so browsers may cache them for a long time.
                context.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            },
        });

        return app;
    }
}
