using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace HamedStack.MessageBus;

/// <summary>
/// Implementation of the IMessageBus interface that processes messages using a middleware pipeline 
/// and routes them to appropriate handlers or subscribers.
/// </summary>
public class MessageBus : IMessageBus
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<Type, HandlerInfo> _handlerRegistry = new();
    private readonly Dictionary<Type, List<Type>> _eventSubscribers = new();
    private readonly List<Func<MessageDelegate, MessageDelegate>> _middleware = new();
    private MessageDelegate _pipeline;

    /// <summary>
    /// Delegate type used in the middleware pipeline for message processing.
    /// </summary>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the handler's response or null.</returns>
    public delegate Task<object?> MessageDelegate(object message, CancellationToken cancellationToken);

    /// <summary>
    /// Initializes a new instance of the MessageBus class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve handler and subscriber instances.</param>
    public MessageBus(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        // Initialize pipeline with the base handler
        _pipeline = InvokeHandler;
    }

    /// <summary>
    /// Registers all handlers and event subscribers from the specified assemblies based on conventions and interfaces.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan for handlers and subscribers. If none provided, all loaded assemblies are scanned.</param>
    public void RegisterHandlers(params Assembly[] assemblies)
    {
        var assemblyList = assemblies.ToList();
        if (!assemblyList.Any())
            assemblyList.AddRange(AppDomain.CurrentDomain.GetAssemblies());

        foreach (var assembly in assemblyList)
        {
            var handlerTypes = assembly.GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false });

            foreach (var handlerType in handlerTypes)
            {
                // Register by convention
                RegisterByConvention(handlerType);

                // Register by interface
                RegisterByInterface(handlerType);
            }
        }
    }

    /// <summary>
    /// Registers handlers and subscribers by convention (methods named "Handle" or "Consume").
    /// </summary>
    /// <param name="handlerType">The type to inspect for registration.</param>
    private void RegisterByConvention(Type handlerType)
    {
        // Register command/query handlers (Handle method)
        var handleMethods = handlerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name == "Handle" && m.GetParameters().Length > 0);

        foreach (var method in handleMethods)
        {
            var messageType = method.GetParameters()[0].ParameterType;
            _handlerRegistry[messageType] = new HandlerInfo
            {
                HandlerType = handlerType,
                MethodInfo = method
            };
        }

        // Register event subscribers (Consume method)
        var consumeMethods = handlerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name == "Consume" && m.GetParameters().Length > 0);

        foreach (var method in consumeMethods)
        {
            var eventType = method.GetParameters()[0].ParameterType;

            if (!_eventSubscribers.ContainsKey(eventType))
                _eventSubscribers[eventType] = new List<Type>();

            if (!_eventSubscribers[eventType].Contains(handlerType))
                _eventSubscribers[eventType].Add(handlerType);
        }
    }

    /// <summary>
    /// Registers handlers and subscribers by interface implementation (IHandler&lt;,&gt;, IHandler&lt;&gt;, IEventSubscriber&lt;&gt;).
    /// </summary>
    /// <param name="handlerType">The type to inspect for registration.</param>
    private void RegisterByInterface(Type handlerType)
    {
        // Check for IHandler<TMessage, TResult> and IHandler<TMessage> implementations
        foreach (var interfaceType in handlerType.GetInterfaces())
        {
            if (!interfaceType.IsGenericType)
                continue;

            var genericTypeDef = interfaceType.GetGenericTypeDefinition();

            // Register command/query handlers
            if (genericTypeDef == typeof(IHandlerTResult<,>) || genericTypeDef == typeof(IHandler<>))
            {
                var messageType = interfaceType.GetGenericArguments()[0];
                var method = interfaceType.GetMethod("Handle");

                if (method != null)
                {
                    _handlerRegistry[messageType] = new HandlerInfo
                    {
                        HandlerType = handlerType,
                        MethodInfo = method
                    };
                }
            }

            // Register event subscribers
            else if (genericTypeDef == typeof(IEventSubscriber<>))
            {
                var eventType = interfaceType.GetGenericArguments()[0];

                if (!_eventSubscribers.ContainsKey(eventType))
                    _eventSubscribers[eventType] = new List<Type>();

                if (!_eventSubscribers[eventType].Contains(handlerType))
                    _eventSubscribers[eventType].Add(handlerType);
            }
        }
    }

    /// <summary>
    /// Adds a middleware to the message processing pipeline.
    /// </summary>
    /// <typeparam name="T">Type of the middleware to add. Must implement IMessageMiddleware.</typeparam>
    public void UseMiddleware<T>() where T : class, IMessageMiddleware
    {
        _middleware.Add(next => async (message, token) =>
        {
            var middleware = _serviceProvider.GetRequiredService<T>();
            return await middleware.InvokeAsync(message, token, next);
        });

        // Rebuild the pipeline
        BuildPipeline();
    }

    /// <summary>
    /// Builds the middleware pipeline by composing all registered middleware components.
    /// </summary>
    private void BuildPipeline()
    {
        MessageDelegate pipeline = InvokeHandler;

        // Build the pipeline in reverse order
        for (int i = _middleware.Count - 1; i >= 0; i--)
        {
            pipeline = _middleware[i](pipeline);
        }

        _pipeline = pipeline;
    }

    /// <summary>
    /// Terminal middleware that invokes the appropriate handler for a message.
    /// </summary>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the handler's response or null.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no handler is registered for the message type or when the handler can't be resolved.</exception>
    private async Task<object?> InvokeHandler(object message, CancellationToken cancellationToken)
    {
        var messageType = message.GetType();

        if (!_handlerRegistry.TryGetValue(messageType, out var handlerInfo))
            throw new InvalidOperationException($"No handler registered for message type {messageType.Name}");

        // Create handler instance
        var handler = _serviceProvider.GetService(handlerInfo.HandlerType);
        if (handler == null)
            throw new InvalidOperationException($"Cannot resolve handler of type {handlerInfo.HandlerType.Name}");

        // Invoke handler method with the message and cancellation token
        var parameters = new List<object> { message };

        // Add cancellation token if the method accepts it
        var methodParams = handlerInfo.MethodInfo.GetParameters();
        if (methodParams.Length > 1 && methodParams[1].ParameterType == typeof(CancellationToken))
            parameters.Add(cancellationToken);

        var result = handlerInfo.MethodInfo.Invoke(handler, parameters.ToArray());

        // Handle different return types
        if (result is Task task)
        {
            await task;

            // If it's Task<T>, extract the result
            if (task.GetType().IsGenericType)
            {
                var resultProperty = task.GetType().GetProperty("Result");
                return resultProperty?.GetValue(task);
            }

            return null;
        }

        return result;
    }

    /// <summary>
    /// Sends a message through the message bus and returns a generic object result.
    /// </summary>
    /// <param name="message">The message to be processed.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the handler's response or null.</returns>
    public async Task<object?> SendAsync(object message, CancellationToken cancellationToken = default)
    {
        return await _pipeline(message, cancellationToken);
    }

    /// <summary>
    /// Sends a message through the message bus and returns a strongly-typed result.
    /// </summary>
    /// <typeparam name="T">The expected return type from the handler.</typeparam>
    /// <param name="message">The message to be processed.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the handler's response cast to type T or default value.</returns>
    public async Task<T?> SendAsync<T>(object message, CancellationToken cancellationToken = default)
    {
        var result = await SendAsync(message, cancellationToken);
        return (T?)result;
    }

    /// <summary>
    /// Publishes an event message to all registered subscribers.
    /// </summary>
    /// <param name="eventMessage">The event message to be published.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task PublishAsync(object eventMessage, CancellationToken cancellationToken = default)
    {
        var eventType = eventMessage.GetType();

        if (!_eventSubscribers.TryGetValue(eventType, out var subscriberTypes) || !subscriberTypes.Any())
            return; // No subscribers, just return

        var tasks = new List<Task>();

        foreach (var subscriberType in subscriberTypes)
        {
            var subscriber = _serviceProvider.GetService(subscriberType);
            if (subscriber == null)
                continue;

            // Try to find the Consume method by convention first
            var consumeMethod = subscriberType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "Consume" && m.GetParameters().Length > 0 &&
                                     m.GetParameters()[0].ParameterType == eventType);

            // If not found by convention, look for interface implementation
            if (consumeMethod == null)
            {
                var eventSubscriberInterface = subscriberType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType &&
                                         i.GetGenericTypeDefinition() == typeof(IEventSubscriber<>) &&
                                         i.GetGenericArguments()[0] == eventType);

                if (eventSubscriberInterface != null)
                    consumeMethod = eventSubscriberInterface.GetMethod("Consume");
            }

            if (consumeMethod == null)
                continue;

            var parameters = new List<object> { eventMessage };

            // Add cancellation token if the method accepts it
            var methodParams = consumeMethod.GetParameters();
            if (methodParams.Length > 1 && methodParams[1].ParameterType == typeof(CancellationToken))
                parameters.Add(cancellationToken);

            var result = consumeMethod.Invoke(subscriber, parameters.ToArray());

            if (result is Task task)
                tasks.Add(task);
        }

        // Wait for all subscribers to process the event
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Contains information about a registered handler.
    /// </summary>
    private class HandlerInfo
    {
        /// <summary>
        /// Gets or sets the handler type.
        /// </summary>
        public required Type HandlerType { get; init; }

        /// <summary>
        /// Gets or sets the method info for the handler method.
        /// </summary>
        public required MethodInfo MethodInfo { get; init; }
    }
}