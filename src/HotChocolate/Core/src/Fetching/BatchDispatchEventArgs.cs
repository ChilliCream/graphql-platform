namespace HotChocolate.Fetching;

/// <summary>
/// Represents an event message from the <see cref="BatchDispatcher"/>.
/// </summary>
/// <param name="Type">The type of event that occurred within the <see cref="IBatchDispatcher"/>.</param>
public readonly record struct BatchDispatchEventArgs(BatchDispatchEventType Type);
