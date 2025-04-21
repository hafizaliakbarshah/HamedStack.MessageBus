namespace HamedStack.MessageBus;

/// <summary>
/// Defines an interface for middleware components in the message processing pipeline.
/// </summary>
public interface IMessageMiddleware
{
    /// <summary>
    /// Processes a message and invokes the next middleware in the pipeline.
    /// </summary>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <param name="next">The next middleware delegate in the pipeline.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the handler's response or null.</returns>
    Task<object?> InvokeAsync(object message, CancellationToken cancellationToken, MessageBus.MessageDelegate next);
}