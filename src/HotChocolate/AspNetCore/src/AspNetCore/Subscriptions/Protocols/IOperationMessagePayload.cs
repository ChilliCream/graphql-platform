namespace HotChocolate.AspNetCore.Subscriptions.Protocols;

/// <summary>
/// The operation message with a custom payload.
/// </summary>
public interface IOperationMessagePayload
{
    /// <summary>
    /// Deserializes the payload as the specified type.
    /// </summary>
    /// <typeparam name="T">
    /// The type as which the payload shall be deserialized.
    /// </typeparam>
    /// <returns>
    /// Returns the deserialized payload.
    /// </returns>
    T? As<T>() where T : class;
}
