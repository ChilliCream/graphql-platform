namespace HotChocolate.Types;

/// <summary>
/// Defines a error factory that translates exceptions into GraphQL errors.
/// This has to be used together with <see cref="ErrorAttribute"/>  or
/// <see cref="ErrorObjectFieldDescriptorExtensions.Error"/>
/// </summary>
/// <typeparam name="TError">
/// The type of the error that is exposed in the API
/// </typeparam>
/// <typeparam name="TException">
/// The exception that should be caught and translated
/// </typeparam>
public interface IPayloadErrorFactory<in TException, out TError> where TException : Exception
{
    /// <summary>
    /// Translates an exception of type <typeparamref name="TException"/> to a GraphQL error of
    /// type <typeparamref name="TError"/>
    /// </summary>
    /// <param name="exception">
    /// The exception that was caught by the error middleware
    /// </param>
    /// <returns>
    /// The translated GraphQL error
    /// </returns>
    TError CreateErrorFrom(TException exception);
}
