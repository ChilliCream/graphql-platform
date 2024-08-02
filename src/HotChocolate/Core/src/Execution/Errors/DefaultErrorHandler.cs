using HotChocolate.Execution.Options;

namespace HotChocolate.Execution.Errors;

internal sealed class DefaultErrorHandler : IErrorHandler
{
    private const string _messageProperty = "message";
    private const string _stackTraceProperty = "stackTrace";

    private readonly IErrorFilter[] _filters;
    private readonly bool _includeExceptionDetails;

    public DefaultErrorHandler(
        IEnumerable<IErrorFilter> errorFilters,
        IErrorHandlerOptionsAccessor options)
    {
        if (errorFilters is null)
        {
            throw new ArgumentNullException(nameof(errorFilters));
        }

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

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
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

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

    public IErrorBuilder CreateUnexpectedError(Exception exception)
    {
        if (exception is null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        return CreateErrorFromException(exception);
    }

    private IErrorBuilder CreateErrorFromException(Exception exception)
    {
        var builder = ErrorBuilder.New()
            .SetMessage("Unexpected Execution Error")
            .SetException(exception);

        if (_includeExceptionDetails)
        {
            builder
                .SetExtension(_messageProperty, exception.Message)
                .SetExtension(_stackTraceProperty, exception.StackTrace);
        }

        return builder;
    }

    public static DefaultErrorHandler Default { get; } =
        new DefaultErrorHandler();
}
