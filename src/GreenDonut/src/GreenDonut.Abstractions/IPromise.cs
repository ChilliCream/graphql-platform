namespace GreenDonut;

/// <summary>
/// Represents a promise that can be canceled.
/// </summary>
public interface IPromise
{
    /// <summary>
    /// Gets the type of the value this promise will produce.
    /// </summary>
    Type Type { get; }

    /// <summary>
    /// Gets the task that represents the async work for this promise.
    /// </summary>
    Task Task { get; }

    /// <summary>
    /// Gets a value indicating whether this promise is a clone.
    /// </summary>
    bool IsClone { get; }

    /// <summary>
    /// Tries to set the result of the async work for this promise.
    /// </summary>
    /// <param name="result"></param>
    void TrySetResult(object? result);

    /// <summary>
    /// Tries to set an exception for the async work for this promise.
    /// </summary>
    /// <param name="exception"></param>
    void TrySetError(Exception exception);

    /// <summary>
    /// Tries to cancel the async work for this promise.
    /// </summary>
    void TryCancel();

    /// <summary>
    /// Clones this promise.
    /// </summary>
    /// <returns>
    /// Returns a new instance of <see cref="IPromise"/>.
    /// </returns>
    IPromise Clone();
}
