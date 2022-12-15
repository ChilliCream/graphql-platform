#nullable enable
namespace HotChocolate.Resolvers;

/// <summary>
/// This helper allows modifying some aspects of the overall operation result object.
/// </summary>
public interface IOperationResultBuilder
{
    /// <summary>
    /// Sets a property on the result context data which can be used for further processing
    /// in the request pipeline.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    void SetResultState(string key, object? value);

    /// <summary>
    /// Sets a property on the result extension data which will
    /// be serialized and send to the consumer.
    ///
    /// <code>
    /// {
    ///    ...
    ///    "extensions": {
    ///       "yourKey": "yourValue"
    ///    }
    /// }
    /// </code>
    ///
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    void SetExtension<TValue>(string key, TValue value);
}
