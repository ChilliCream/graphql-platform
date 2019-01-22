using System;
using System.Collections.Generic;
using HotChocolate.Execution;
using HotChocolate.Resolvers;

namespace HotChocolate.Stitching
{
    internal static class ContextDataExtensions
    {
        private const string _variables = "__hc_variables";

        public static IReadOnlyDictionary<string, object> GetVariables(
            this IMiddlewareContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.ContextData.TryGetValue(_variables, out object obj)
                && obj is IReadOnlyDictionary<string, object> variables)
            {
                return variables;
            }
            return new Dictionary<string, object>();
        }

        public static void SetVariables(
            this IQueryContext context,
            IReadOnlyDictionary<string, object> variables)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (variables == null)
            {
                throw new ArgumentNullException(nameof(variables));
            }

            context.ContextData[_variables] = variables;
        }
    }
}
