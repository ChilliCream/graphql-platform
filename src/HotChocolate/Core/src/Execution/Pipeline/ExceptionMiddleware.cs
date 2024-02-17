using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Pipeline;

internal sealed class ExceptionMiddleware(RequestDelegate next, [SchemaService] IErrorHandler errorHandler)
{
    private readonly RequestDelegate _next = next ?? 
        throw new ArgumentNullException(nameof(next));
    private readonly IErrorHandler _errorHandler = errorHandler ?? 
        throw new ArgumentNullException(nameof(errorHandler));

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
    
    public static RequestCoreMiddleware Create()
        => (core, next) =>
        {
            var errorHandler = core.SchemaServices.GetRequiredService<IErrorHandler>();
            var middleware = new ExceptionMiddleware(next, errorHandler);
            return context => middleware.InvokeAsync(context);
        };
}
