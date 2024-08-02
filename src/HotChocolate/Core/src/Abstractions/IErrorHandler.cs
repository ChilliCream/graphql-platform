namespace HotChocolate;

/// <summary>
/// The error handler is used to apply error filters onto raised errors.
/// </summary>
public interface IErrorHandler
{
    /// <summary>
    /// Apply error filter.
    /// </summary>
    /// <param name="error">
    /// The raised error object.
    /// </param>
    /// <returns>
    /// The error object to which all filters where applied.
    /// </returns>
    IError Handle(IError error);

    /// <summary>
    /// Creates an error from an unexpected exception.
    /// </summary>
    /// <param name="exception">
    /// The exception from which to create an error builder.
    /// </param>
    /// <returns>
    /// The error builder that can be used to tweak and build the error object.
    /// </returns>
    IErrorBuilder CreateUnexpectedError(Exception exception);
}
