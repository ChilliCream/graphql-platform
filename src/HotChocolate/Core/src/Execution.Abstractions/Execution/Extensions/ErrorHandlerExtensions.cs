namespace HotChocolate.Execution;

/// <summary>
/// Provides extension methods for <see cref="IErrorHandler"/>.
/// </summary>
public static class ErrorHandlerExtensions
{
    /// <summary>
    /// Handles a list of errors.
    /// </summary>
    /// <param name="errorHandler">The error handler.</param>
    /// <param name="errors">The errors to handle.</param>
    /// <returns>The handled errors.</returns>
    public static IReadOnlyList<IError> Handle(this IErrorHandler errorHandler, IReadOnlyList<IError> errors)
    {
        ArgumentNullException.ThrowIfNull(errorHandler);
        ArgumentNullException.ThrowIfNull(errors);

        var result = new List<IError>();

        foreach (var error in errors)
        {
            if (error is AggregateError aggregateError)
            {
                foreach (var innerError in aggregateError.Errors)
                {
                    AddProcessed(errorHandler.Handle(innerError));
                }
            }
            else
            {
                AddProcessed(errorHandler.Handle(error));
            }
        }

        return result;

        void AddProcessed(IError error)
        {
            if (error is AggregateError aggregateError)
            {
                foreach (var innerError in aggregateError.Errors)
                {
                    result.Add(innerError);
                }
            }
            else
            {
                result.Add(error);
            }
        }
    }
}