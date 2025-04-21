namespace HamedStack.MessageBus;

/// <summary>
/// Defines a handler interface for processing messages that don't return a result.
/// </summary>
/// <typeparam name="TMessage">The type of message to handle.</typeparam>
public interface IHandler<in TMessage>
{
    /// <summary>
    /// Handles the specified message without returning a result.
    /// </summary>
    /// <param name="message">The message to handle.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task Handle(TMessage message, CancellationToken cancellationToken = default);
}