using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Validation;

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
                context.Result = QueryResult.CreateError(new QueryError(
                   "The coerce variables middleware expects the " +
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
