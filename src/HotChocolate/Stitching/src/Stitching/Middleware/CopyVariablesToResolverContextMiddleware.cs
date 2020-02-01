using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching
{
    public class CopyVariablesToResolverContextMiddleware
    {
        private readonly QueryDelegate _next;

        public CopyVariablesToResolverContextMiddleware(QueryDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public Task InvokeAsync(IQueryContext context)
        {
            IVariableValueCollection variables = context.Operation.Variables;
            var dict = new Dictionary<string, IValueNode>();

            foreach (string key in context.Request.VariableValues.Keys)
            {
                dict.Add(key, variables.GetVariable<IValueNode>(key));
            }

            context.SetVariables(dict);

            return _next.Invoke(context);
        }
    }
}
