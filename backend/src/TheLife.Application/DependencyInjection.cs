using System.Reflection;
using FluentValidation;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Features.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace TheLife.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers every validator and every command/query handler in this project.
    /// New feature slices are picked up automatically: add the file and it just works.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        RegisterHandlers(services, assembly, typeof(ICommandHandler<,>), typeof(ValidatingCommandHandler<,>));
        RegisterHandlers(services, assembly, typeof(IQueryHandler<,>), typeof(ValidatingQueryHandler<,>));

        // Shared helpers used by several slices.
        services.AddScoped<SessionIssuer>();

        return services;
    }

    /// <summary>
    /// For every class implementing <paramref name="handlerInterface"/>, e.g. CreatePostHandler:
    ///   * registers the class itself, and
    ///   * registers the interface (ICommandHandler&lt;CreatePostCommand, PostDto&gt;) as the handler
    ///     wrapped in <paramref name="validatingDecorator"/>.
    /// So whoever asks for the interface always gets validation first.
    /// </summary>
    private static void RegisterHandlers(
        IServiceCollection services, Assembly assembly, Type handlerInterface, Type validatingDecorator)
    {
        var handlers =
            from type in assembly.GetTypes()
            where type is { IsClass: true, IsAbstract: false, ContainsGenericParameters: false }
            from implemented in type.GetInterfaces()
            where implemented.IsGenericType && implemented.GetGenericTypeDefinition() == handlerInterface
            select (Implementation: type, Service: implemented);

        foreach (var (implementation, service) in handlers)
        {
            services.AddScoped(implementation);

            var decoratorType = validatingDecorator.MakeGenericType(service.GetGenericArguments());
            services.AddScoped(service, provider =>
                ActivatorUtilities.CreateInstance(provider, decoratorType, provider.GetRequiredService(implementation)));
        }
    }
}
