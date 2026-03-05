using Mocha;

namespace Mocha.Sagas;

/// <summary>
/// A middleware that intercepts saga event handling, allowing cross-cutting concerns
/// such as logging, error handling, or metrics to be applied around saga transitions.
/// </summary>
public interface ISagaEventMiddleware
{
    /// <summary>
    /// Handles an event directed at a saga, optionally delegating to the next handler in the pipeline.
    /// </summary>
    /// <typeparam name="TEvent">The type of event being handled.</typeparam>
    /// <param name="event">The event to handle.</param>
    /// <param name="context">The consume context for the current message.</param>
    /// <param name="next">The next handler in the middleware pipeline.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task HandleEvent<TEvent>(TEvent @event, IConsumeContext context, Func<TEvent, IConsumeContext, Task> next)
        where TEvent : notnull;
}
