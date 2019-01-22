using System;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.Stitching
{
    public class CopyVariablesToResolverContext
    {
        private QueryDelegate _next;

        public CopyVariablesToResolverContext(QueryDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public Task InvokeAsync(IQueryContext context)
        {
            context.SetVariables(context.Request.VariableValues);
            return _next.Invoke(context);
        }
    }
}
