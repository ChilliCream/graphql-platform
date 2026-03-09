namespace HotChocolate.Events.Contracts;

/// <summary>
/// Represents a handler for a schema validation event.
/// </summary>
/// <typeparam name="TEvent">The type of event to handle.</typeparam>
public interface IValidationEventHandler<in TEvent> where TEvent : IValidationEvent
{
    /// <summary>
    /// Handles the specified schema validation event.
    /// </summary>
    /// <param name="event">The schema validation event to handle.</param>
    /// <param name="context">The validation context.</param>
    void Handle(TEvent @event, ValidationContext context);
}
