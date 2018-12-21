using System;
using System.Threading.Tasks;
using HotChocolate.Validation;

namespace HotChocolate.Execution
{
    internal sealed class ExceptionMiddleware
    {
        private readonly QueryDelegate _next;
        private readonly IErrorHandler _errorHandler;

        public ExceptionMiddleware(
            QueryDelegate next,
            IErrorHandler errorHandler)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _errorHandler = errorHandler ?? ErrorHandler.Default;
        }

        public async Task InvokeAsync(IQueryContext context)
        {
            try
            {
                await _next(context);
            }
            catch (QueryException ex)
            {
                context.Exception = ex;
                context.Result = new QueryResult(
                    _errorHandler.Handle(ex.Errors));
            }
            catch (Exception ex)
            {
                context.Exception = ex;
                context.Result = new QueryResult(
                    _errorHandler.Handle(ex, error => error));
            }
        }
    }
}

