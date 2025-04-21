namespace HamedStack.MessageBus;

/// <summary>
/// Defines a message bus interface for handling commands, queries, and events in a distributed application.
/// </summary>
public interface IMessageBus
{
    /// <summary>
    /// Sends a message through the message bus and returns a generic object result.
    /// </summary>
    /// <param name="message">The message to be processed.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the handler's response or null.</returns>
    Task<object?> SendAsync(object message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message through the message bus and returns a strongly-typed result.
    /// </summary>
    /// <typeparam name="T">The expected return type from the handler.</typeparam>
    /// <param name="message">The message to be processed.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the handler's response cast to type T or default value.</returns>
    Task<T?> SendAsync<T>(object message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes an event message to all registered subscribers.
    /// </summary>
    /// <param name="eventMessage">The event message to be published.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PublishAsync(object eventMessage, CancellationToken cancellationToken = default);
}