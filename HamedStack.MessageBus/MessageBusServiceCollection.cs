using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace HamedStack.MessageBus;

/// <summary>
/// Extension methods for IServiceCollection to register and configure the message bus.
/// </summary>
public static class MessageBusServiceCollection
{
    /// <summary>
    /// Adds the message bus and its dependencies to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the message bus to.</param>
    /// <param name="configure">Optional configuration action for the message bus.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddMessageBus(this IServiceCollection services,
        Action<MessageBusBuilder>? configure = null)
    {
        // Register all handler and subscriber classes
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            // Find by convention
            var handlerTypes = assembly.GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false })
                .Where(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Any(m => m.Name is "Handle" or "Consume" && m.GetParameters().Length > 0));

            // Find by interface implementation
            var interfaceHandlerTypes = assembly.GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false })
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType && (
                        i.GetGenericTypeDefinition() == typeof(IHandlerTResult<,>) ||
                        i.GetGenericTypeDefinition() == typeof(IHandler<>) ||
                        i.GetGenericTypeDefinition() == typeof(IEventSubscriber<>)
                    )
                ));

            var allHandlerTypes = handlerTypes.Union(interfaceHandlerTypes);

            foreach (var handlerType in allHandlerTypes)
            {
                services.AddTransient(handlerType);
            }
        }

        // Configure the message bus
        var builder = new MessageBusBuilder(services);
        configure?.Invoke(builder);

        // Register the concrete MessageBus implementation
        services.AddSingleton(provider =>
        {
            var bus = new MessageBus(provider);

            // Apply default handlers if none specified
            if (!builder.Middleware.Any(m => m.Method.Name.Contains("RegisterHandlers")))
            {
                bus.RegisterHandlers(assemblies);
            }

            // Apply middleware from builder
            foreach (var middleware in builder.Middleware)
            {
                middleware(bus);
            }

            return bus;
        });

        // Register the interface with the implementation
        services.AddSingleton<IMessageBus>(provider => provider.GetRequiredService<MessageBus>());

        return services;
    }
}