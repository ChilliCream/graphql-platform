using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Pipeline;

internal sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IErrorHandler _errorHandler;

    private ExceptionMiddleware(RequestDelegate next, [SchemaService] IErrorHandler errorHandler)
    {
        _next = next ??
            throw new ArgumentNullException(nameof(next));
        _errorHandler = errorHandler ??
            throw new ArgumentNullException(nameof(errorHandler));
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
            context.Result = OperationResultBuilder.CreateError(_errorHandler.Handle(ex.Errors));
        }
        catch (Exception ex)
        {
            context.Exception = ex;
            var error = _errorHandler.CreateUnexpectedError(ex).Build();
            context.Result = OperationResultBuilder.CreateError(_errorHandler.Handle(error));
        }
    }

    public static RequestCoreMiddleware Create()
        => (core, next) =>
        {
            var errorHandler = core.SchemaServices.GetRequiredService<IErrorHandler>();
            var middleware = Create(next, errorHandler);
            return context => middleware.InvokeAsync(context);
        };

    internal static ExceptionMiddleware Create(
        RequestDelegate next,
        IErrorHandler errorHandler)
        => new(next, errorHandler);
}