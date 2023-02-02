using System.Linq.Expressions;
using System.Reflection;
using Marten;

namespace HotChocolate.Data.Marten.Filtering;

/// <summary>
/// Helper methods to build marten query expressions.
/// </summary>
internal static class MartenExpressionHelper
{
    private static readonly MethodInfo _in =
        typeof(LinqExtensions)
            .GetMethods()
            .Single(m =>
                m.Name.Equals(nameof(LinqExtensions.In))
                && m.GetParameters().Length == 2
                && m.GetParameters().Last().ParameterType.IsGenericType
                && m.GetParameters()
                    .Last()
                    .ParameterType
                    .GetGenericTypeDefinition() == typeof(IList<>));

    /// <summary>
    /// A helper to build the marten in operation expression.
    /// </summary>
    public static Expression In(
        Expression property,
        Type genericType,
        object? parsedValue)
    {
        return Expression.Call(
            _in.MakeGenericMethod(genericType),
            property,
            Expression.Convert(
                Expression.Constant(parsedValue),
                typeof(IList<>).MakeGenericType(genericType)));
    }
}
