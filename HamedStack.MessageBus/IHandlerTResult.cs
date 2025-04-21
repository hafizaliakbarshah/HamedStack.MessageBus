namespace HamedStack.MessageBus;

/// <summary>
/// Defines a handler interface for processing messages that return a result.
/// </summary>
/// <typeparam name="TMessage">The type of message to handle.</typeparam>
/// <typeparam name="TResult">The type of result returned by the handler.</typeparam>
public interface IHandlerTResult<in TMessage, TResult>
{
    /// <summary>
    /// Handles the specified message and returns a result.
    /// </summary>
    /// <param name="message">The message to handle.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the handler's response.</returns>
    Task<TResult> Handle(TMessage message, CancellationToken cancellationToken = default);
}