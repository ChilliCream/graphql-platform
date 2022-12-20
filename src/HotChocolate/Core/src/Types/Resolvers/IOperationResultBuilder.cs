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
    /// Sets a property on the result context data which can be used for further processing
    /// in the request pipeline.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">
    /// A delegate to create the value for the state or
    /// to mutate the current value stored on the state.
    /// </param>
    void SetResultState(string key, UpdateState<object?> value);

    /// <summary>
    /// Sets a property on the result context data which can be used for further processing
    /// in the request pipeline.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="state">A state value that can be passed into the delegate.</param>
    /// <param name="value">
    /// A delegate to create the value for the state or
    /// to mutate the current value stored on the state.
    /// </param>
    void SetResultState<TState>(string key, TState state, UpdateState<object?, TState> value);

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
    /// <param name="value">
    /// A delegate to create the value for the extensions or
    /// to mutate the current value stored on the extensions.
    /// </param>
    void SetExtension<TValue>(string key, UpdateState<TValue> value);

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
    /// <param name="state">A state value that can be passed into the delegate.</param>
    /// <param name="value">
    /// A delegate to create the value for the extensions or
    /// to mutate the current value stored on the extensions.
    /// </param>
    void SetExtension<TValue, TState>(string key, TState state, UpdateState<TValue, TState> value);
}

/// <summary>
/// A delegate used to update execution state.
/// </summary>
/// <typeparam name="TValue">The type of the value stored in the state.</typeparam>
public delegate TValue UpdateState<TValue>(
    string key,
    TValue currentValue);

/// <summary>
/// A delegate used to update execution state.
/// </summary>
/// <typeparam name="TValue">The type of the value stored in the state.</typeparam>
/// <typeparam name="TState">A state object that will be passed into the delegate.</typeparam>
public delegate TValue UpdateState<TValue, in TState>(
    string key,
    TValue currentValue,
    TState state);
