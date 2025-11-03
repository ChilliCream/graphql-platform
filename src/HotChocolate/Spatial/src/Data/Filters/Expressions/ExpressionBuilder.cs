using System.Linq.Expressions;
using System.Reflection;
using NetTopologySuite.Geometries;

namespace HotChocolate.Data.Filters.Spatial;

internal static class ExpressionBuilder
{
    private static readonly MethodInfo s_contains =
        typeof(Geometry).GetMethod(nameof(Geometry.Contains))!;
    private static readonly MethodInfo s_distance =
        typeof(Geometry).GetMethod(nameof(Geometry.Distance))!;
    private static readonly MethodInfo s_intersects =
        typeof(Geometry).GetMethod(nameof(Geometry.Intersects))!;
    private static readonly MethodInfo s_overlaps =
        typeof(Geometry).GetMethod(nameof(Geometry.Overlaps))!;
    private static readonly MethodInfo s_touches =
        typeof(Geometry).GetMethod(nameof(Geometry.Touches))!;
    private static readonly MethodInfo s_within =
        typeof(Geometry).GetMethod(nameof(Geometry.Within))!;

    private static readonly MethodInfo s_buffer =
        typeof(Geometry)
            .GetMethods()
            .Single(m => m.Name.Equals(nameof(Geometry.Buffer)) && m.GetParameters().Length == 1);

    public static Expression Buffer(Geometry geometry, double buffer) =>
        Expression.Call(
            CreateAndConvertParameter<Geometry>(geometry),
            s_buffer,
            CreateAndConvertParameter<double>(buffer));

    public static Expression Contains(Expression property, Expression geometry) =>
        Expression.Call(property, s_contains, geometry);

    public static Expression Contains(Expression property, Geometry geometry) =>
        Expression.Call(
            property,
            s_contains,
            CreateAndConvertParameter<Geometry>(geometry));

    public static Expression Distance(Expression property, Expression geometry) =>
        Expression.Call(property, s_distance, geometry);

    public static Expression Distance(Expression property, Geometry geometry) =>
        Expression.Call(
            property,
            s_distance,
            CreateAndConvertParameter<Geometry>(geometry));

    public static Expression Intersects(Expression property, Expression geometry) =>
        Expression.Call(property, s_intersects, geometry);

    public static Expression Intersects(Expression property, Geometry geometry) =>
        Expression.Call(
            property,
            s_intersects,
            CreateAndConvertParameter<Geometry>(geometry));

    public static Expression Overlaps(Expression property, Expression geometry) =>
        Expression.Call(property, s_overlaps, geometry);

    public static Expression Overlaps(Expression property, Geometry geometry) =>
        Expression.Call(
            property,
            s_overlaps,
            CreateAndConvertParameter<Geometry>(geometry));

    public static Expression Touches(Expression property, Expression geometry) =>
        Expression.Call(property, s_touches, geometry);

    public static Expression Touches(Expression property, Geometry geometry) =>
        Expression.Call(
            property,
            s_touches,
            CreateAndConvertParameter<Geometry>(geometry));

    public static Expression Within(Expression property, Expression geometry) =>
        Expression.Call(property, s_within, geometry);

    public static Expression Within(Expression property, Geometry geometry) =>
        Expression.Call(
            property,
            s_within,
            CreateAndConvertParameter<Geometry>(geometry));

    private static Expression CreateAndConvertParameter<T>(object value)
    {
        Expression<Func<T>> lambda = () => (T)value;
        return lambda.Body;
    }
}
