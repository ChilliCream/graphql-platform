using HotChocolate.Execution.Options;

namespace HotChocolate.Execution.Errors;

internal sealed class DefaultErrorHandler : IErrorHandler
{
    private const string MessageProperty = "message";
    private const string StackTraceProperty = "stackTrace";

    private readonly IErrorFilter[] _filters;
    private readonly bool _includeExceptionDetails;

    public DefaultErrorHandler(
        IEnumerable<IErrorFilter> errorFilters,
        IErrorHandlerOptionsAccessor options)
    {
        ArgumentNullException.ThrowIfNull(errorFilters);
        ArgumentNullException.ThrowIfNull(options);

        _filters = errorFilters.ToArray();
        _includeExceptionDetails = options.IncludeExceptionDetails;
    }

    private DefaultErrorHandler()
    {
        _filters = [];
        _includeExceptionDetails = false;
    }

    public IError Handle(IError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var current = error;

        foreach (var filter in _filters)
        {
            current = filter.OnError(current);

            if (current is null)
            {
                throw new InvalidOperationException(
                    "Unexpected Execution Error");
            }
        }

        return current;
    }

    public ErrorBuilder CreateUnexpectedError(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return CreateErrorFromException(exception);
    }

    private ErrorBuilder CreateErrorFromException(Exception exception)
    {
        var builder = ErrorBuilder.New()
            .SetMessage("Unexpected Execution Error")
            .SetException(exception);

        if (_includeExceptionDetails)
        {
            builder
                .SetExtension(MessageProperty, exception.Message)
                .SetExtension(StackTraceProperty, exception.StackTrace);
        }

        return builder;
    }

    public static DefaultErrorHandler Default { get; } = new();
}
