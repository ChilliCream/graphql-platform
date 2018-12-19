using System;
using System.Threading.Tasks;
using HotChocolate.Validation;

namespace HotChocolate.Execution
{
    internal sealed class ExceptionMiddleware
    {
        public readonly QueryDelegate _next;
        public readonly IQueryValidator _validator;

        public ExceptionMiddleware(QueryDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
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
                context.Result = new QueryResult(ex.Errors);
            }
            catch (Exception ex)
            {
                context.Exception = ex;
                context.Result = new QueryResult(
                    CreateErrorFromException(context.Schema, ex));
            }
        }

        private IError CreateErrorFromException(
            ISchema schema,
            Exception exception)
        {
            if (schema.Options.DeveloperMode)
            {
                return new QueryError(
                    $"{exception.Message}\r\n\r\n{exception.StackTrace}");
            }
            else
            {
                return new QueryError("Unexpected execution error.");
            }
        }
    }
}

