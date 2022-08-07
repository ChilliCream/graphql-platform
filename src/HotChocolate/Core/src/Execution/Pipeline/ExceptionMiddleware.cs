using System;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Pipeline;

internal sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IErrorHandler _errorHandler;

    public ExceptionMiddleware(RequestDelegate next, IErrorHandler errorHandler)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
    }

    public async ValueTask InvokeAsync(IRequestContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            context.Exception = ex;
            context.Result = ErrorHelper.OperationCanceled();
        }
        catch (GraphQLException ex)
        {
            context.Exception = ex;
            context.Result = QueryResultBuilder.CreateError(_errorHandler.Handle(ex.Errors));
        }
        catch (Exception ex)
        {
            context.Exception = ex;
            var error = _errorHandler.CreateUnexpectedError(ex).Build();
            context.Result = QueryResultBuilder.CreateError(_errorHandler.Handle(error));
        }
    }
}

