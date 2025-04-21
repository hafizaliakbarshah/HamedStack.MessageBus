namespace HamedStack.MessageBus;

/// <summary>
/// Defines an event subscriber interface for consuming events.
/// </summary>
/// <typeparam name="TEvent">The type of event to consume.</typeparam>
public interface IEventSubscriber<in TEvent>
{
    /// <summary>
    /// Consumes the specified event message.
    /// </summary>
    /// <param name="eventMessage">The event message to consume.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task Consume(TEvent eventMessage, CancellationToken cancellationToken = default);
}