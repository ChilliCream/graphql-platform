namespace HotChocolate.Fetching;

/// <summary>
/// Represents the type of event that can occur within the <see cref="IBatchDispatcher"/>.
/// </summary>
public enum BatchDispatchEventType
{
    /// <summary>
    /// A batch was enqueued to the <see cref="IBatchDispatcher"/>.
    /// </summary>
    Enqueued,

    /// <summary>
    /// A batch was evaluated by the <see cref="IBatchDispatcher"/>.
    /// </summary>
    Evaluated,

    /// <summary>
    /// A batch was dispatched by the <see cref="IBatchDispatcher"/>.
    /// </summary>
    Dispatched
}
