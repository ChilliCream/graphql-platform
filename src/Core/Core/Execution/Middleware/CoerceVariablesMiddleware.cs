using System;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    public class CoerceVariablesMiddleware
    {
        private readonly QueryDelegate _next;

        public CoerceVariablesMiddleware(QueryDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public Task InvokeAsync(IQueryContext context)
        {
            if (!IsContextValid(context))
            {
                context.Result = new QueryResult(new QueryError(
                   "The coerce variables middleware expectes the " +
                   "query document to be parsed and the operation " +
                   "to be resolved."));
                return Task.CompletedTask;
            }

            var variableBuilder = new VariableValueBuilder(
                context.Schema, context.Operation.Definition);
            context.Variables = variableBuilder.CreateValues(
                context.Request.VariableValues);

            return _next(context);
        }

        private bool IsContextValid(IQueryContext context)
        {
            return context.Document != null
                && context.Operation != null;
        }
    }
}

