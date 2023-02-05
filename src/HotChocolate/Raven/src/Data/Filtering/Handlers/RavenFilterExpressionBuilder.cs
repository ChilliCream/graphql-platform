using System.Linq.Expressions;
using System.Reflection;
using Raven.Client.Documents.Linq;

namespace HotChocolate.Data.Raven.Filtering.Handlers;

public class RavenFilterExpressionBuilder
{
    private static readonly MethodInfo _inMethod =
        typeof(RavenQueryableExtensions)
            .GetMethods()
            .Single(x => x.Name == nameof(RavenQueryableExtensions.In) &&
                x.GetParameters() is [_, { ParameterType.IsArray: false }]);

    public static Expression In(
        Expression property,
        Type genericType,
        object? parsedValue)
    {
        return Expression.Call(
            _inMethod.MakeGenericMethod(genericType),
            new[] { property, Expression.Constant(parsedValue), });
    }
}
