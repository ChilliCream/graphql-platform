using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Raven.Client.Documents.Linq;

namespace HotChocolate.Data.Raven.Filtering.Handlers;

public class RavenFilterExpressionBuilder
{
    private static readonly MethodInfo _inMethod =
        typeof(RavenQueryableExtensions)
            .GetMethods()
            .Single(x => x.Name == nameof(RavenQueryableExtensions.In) &&
                x.GetParameters() is [_, { ParameterType.IsArray: false, },]);

    private static readonly MethodInfo _isMatch =
        typeof(Regex)
            .GetMethods()
            .Single(x => x.Name == nameof(Regex.IsMatch) &&
                x.GetParameters() is [{ } first, { } second,] &&
                first.ParameterType == typeof(string) &&
                second.ParameterType == typeof(string));

    public static Expression In(
        Expression property,
        Type genericType,
        object? parsedValue)
    {
        return Expression.Call(
            _inMethod.MakeGenericMethod(genericType),
            [property, Expression.Constant(parsedValue),]);
    }

    public static Expression IsMatch(
        Expression property,
        string parsedValue)
    {
        parsedValue = $".*{Regex.Escape(parsedValue)}.*";

        return Expression.Call(
            _isMatch,
            [property, Expression.Constant(parsedValue),]);
    }
}
