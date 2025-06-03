namespace HotChocolate.Execution;

public static class ErrorHandlerExtensions
{
    public static IReadOnlyList<IError> Handle(this IErrorHandler errorHandler, IReadOnlyList<IError> errors)
    {
        ArgumentNullException.ThrowIfNull(errorHandler);
        ArgumentNullException.ThrowIfNull(errors);

        var rewrittenErrors = new IError[errors.Count];

        for (var i = 0; i < errors.Count; i++)
        {
            rewrittenErrors[i] = errorHandler.Handle(errors[i]);
        }

        return rewrittenErrors;
    }
}