using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace HamedStack.MessageBus;

/// <summary>
/// Builder class for configuring a message bus instance.
/// </summary>
public class MessageBusBuilder
{
    private readonly IServiceCollection _services;

    /// <summary>
    /// Gets the list of middleware configuration actions.
    /// </summary>
    internal List<Action<MessageBus>> Middleware { get; } = new();

    /// <summary>
    /// Initializes a new instance of the MessageBusBuilder class.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    public MessageBusBuilder(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// Adds a middleware to the message processing pipeline.
    /// </summary>
    /// <typeparam name="T">Type of the middleware to add. Must implement IMessageMiddleware.</typeparam>
    /// <returns>The builder instance for method chaining.</returns>
    public MessageBusBuilder UseMiddleware<T>() where T : class, IMessageMiddleware
    {
        _services.AddTransient<T>();
        Middleware.Add(bus => bus.UseMiddleware<T>());
        return this;
    }

    /// <summary>
    /// Registers all handlers and event subscribers from the specified assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan for handlers and subscribers.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public MessageBusBuilder RegisterHandlers(params Assembly[] assemblies)
    {
        Middleware.Add(bus => bus.RegisterHandlers(assemblies));
        return this;
    }
}