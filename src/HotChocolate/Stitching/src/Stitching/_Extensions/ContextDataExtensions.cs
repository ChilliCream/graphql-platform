using System.Collections.Generic;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;

#nullable enable

namespace HotChocolate.Stitching
{
    internal static class ContextDataExtensions_legacy
    {
        private const string _variables = "HotChocolate.Stitching.Variables";
        private static readonly Dictionary<string, IValueNode> _empty =
            new Dictionary<string, IValueNode>();

        public static IReadOnlyDictionary<string, IValueNode> GetVariables(
            this IMiddlewareContext context)
        {
            if (context.ContextData.TryGetValue(_variables, out object? obj)
                && obj is IReadOnlyDictionary<string, IValueNode> variables)
            {
                return variables;
            }
            return _empty;
        }

        public static void SetVariables(
            this IQueryContext context,
            IReadOnlyDictionary<string, IValueNode> variables) =>
            context.ContextData[_variables] = variables;
    }
}
