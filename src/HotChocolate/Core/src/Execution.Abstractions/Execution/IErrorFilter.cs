namespace HotChocolate.Execution;

/// <summary>
/// An error filter can handle and rewrite errors that occurred
/// during execution.
/// </summary>
public interface IErrorFilter
{
    /// <summary>
    /// OnError is called whenever an error occurred during
    /// execution of a query.
    /// </summary>
    /// <param name="error">
    /// The error that occurred. This argument cannot be null.
    /// </param>
    /// <returns>
    /// Returns the error passed in to this filter or a rewritten error.
    /// It is not allowed to return null.
    /// </returns>
    IError OnError(IError error);
}
