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
            var variableBuilder = new VariableValueBuilder(
                context.Schema, context.Operation.Definition);
            context.Variables = variableBuilder.CreateValues(
                context.Request.VariableValues);
            return _next(context);
        }
    }
}

