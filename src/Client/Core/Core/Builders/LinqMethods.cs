using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Client.Core.Builders
{
    internal static class LinqMethods
    {
        public static readonly MethodInfo SelectManyMethod = GetMethodInfoOf(() =>
            Enumerable.SelectMany<object, object>(default, default(Func<object, IEnumerable<object>>)))
                .GetGenericMethodDefinition();
        public static readonly MethodInfo ToDictionaryMethod = typeof(Enumerable)
            .GetTypeInfo()
            .GetDeclaredMethods(nameof(Enumerable.ToDictionary))
            .Single(x => x.GetGenericArguments().Length == 3 && x.GetParameters().Length == 3);
        public static readonly MethodInfo ToListMethod = typeof(Enumerable).GetTypeInfo().GetDeclaredMethod(nameof(Enumerable.ToList));

        private static MethodInfo GetMethodInfoOf<T>(Expression<Func<T>> expression)
        {
            var body = (MethodCallExpression)expression.Body;
            return body.Method;
        }
    }
}
