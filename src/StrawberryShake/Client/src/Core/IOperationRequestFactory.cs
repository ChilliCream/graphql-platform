namespace StrawberryShake;

/// <summary>
/// The operation request factory allows to create requests for specific operations.
/// </summary>
public interface IOperationRequestFactory
{
    /// <summary>
    /// The result type of the operation.
    /// </summary>
    Type ResultType { get; }

    /// <summary>
    /// Creates an operation request with the given variables.
    /// </summary>
    /// <param name="variables">
    /// The variables that shall be passed into the request.
    /// </param>
    /// <returns>
    /// Returns a new operation request.
    /// </returns>
    OperationRequest Create(IReadOnlyDictionary<string, object?>? variables);
}
