namespace HotChocolate.Execution;

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
}
