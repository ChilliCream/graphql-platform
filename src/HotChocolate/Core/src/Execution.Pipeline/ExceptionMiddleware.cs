using HotChocolate.Execution.Instrumentation;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Pipeline;

internal sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IErrorHandler _errorHandler;
    private readonly ICoreExecutionDiagnosticEvents _diagnosticEvents;

    private ExceptionMiddleware(
        RequestDelegate next,
        ICoreExecutionDiagnosticEvents diagnosticEvents,
        IErrorHandler errorHandler)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(diagnosticEvents);
        ArgumentNullException.ThrowIfNull(errorHandler);

        _next = next;
        _diagnosticEvents = diagnosticEvents;
        _errorHandler = errorHandler;
    }

    public async ValueTask InvokeAsync(RequestContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            var error = _errorHandler.Handle(ErrorHelper.OperationCanceled(ex));
            context.Result = OperationResultBuilder.CreateError(error);
            _diagnosticEvents.ExecutionError(context, ErrorKind.RequestError, [error]);
        }
        catch (GraphQLException ex)
        {
            var errors = _errorHandler.Handle(ex.Errors);
            context.Result = OperationResultBuilder.CreateError(errors);
            _diagnosticEvents.ExecutionError(context, ErrorKind.RequestError, errors);
        }
        catch (Exception ex)
        {
            var error = _errorHandler.Handle(ErrorBuilder.FromException(ex).Build());
            context.Result = OperationResultBuilder.CreateError(error);
            _diagnosticEvents.ExecutionError(context, ErrorKind.RequestError, [error]);
        }
    }

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            (core, next) =>
            {
                var errorHandler = core.SchemaServices.GetRequiredService<IErrorHandler>();
                var diagnosticEvents = core.SchemaServices.GetRequiredService<ICoreExecutionDiagnosticEvents>();
                var middleware = Create(next, diagnosticEvents, errorHandler);
                return context => middleware.InvokeAsync(context);
            },
            nameof(ExceptionMiddleware));

    internal static ExceptionMiddleware Create(
        RequestDelegate next,
        ICoreExecutionDiagnosticEvents diagnosticEvents,
        IErrorHandler errorHandler)
        => new(next, diagnosticEvents, errorHandler);
}
