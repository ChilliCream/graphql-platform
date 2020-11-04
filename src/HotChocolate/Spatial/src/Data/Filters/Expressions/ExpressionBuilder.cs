using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NetTopologySuite.Geometries;

namespace HotChocolate.Data.Filters.Spatial
{
    internal static class ExpressionBuilder
    {
        private static readonly MethodInfo _contains =
            typeof(Geometry).GetMethod(nameof(Geometry.Contains))!;
        private static readonly MethodInfo _distance =
            typeof(Geometry).GetMethod(nameof(Geometry.Distance))!;
        private static readonly MethodInfo _intersects =
            typeof(Geometry).GetMethod(nameof(Geometry.Intersects))!;
        private static readonly MethodInfo _overlaps =
            typeof(Geometry).GetMethod(nameof(Geometry.Overlaps))!;
        private static readonly MethodInfo _touches =
            typeof(Geometry).GetMethod(nameof(Geometry.Touches))!;
        private static readonly MethodInfo _within =
            typeof(Geometry).GetMethod(nameof(Geometry.Within))!;

        private static readonly MethodInfo _buffer =
            typeof(Geometry)
                .GetMethods()
                .Single(m => m.Name.Equals(nameof(Geometry.Buffer)) &&
                    m.GetParameters().Length == 1);

        public static Expression Buffer(Geometry geometry, double buffer) =>
            Expression.Call(
                CreateAndConvertParameter<Geometry>(geometry),
                _buffer,
                CreateAndConvertParameter<double>(buffer));

        public static Expression Contains(Expression property, Expression geometry) =>
            Expression.Call(property, _contains, geometry);

        public static Expression Contains(Expression property, Geometry geometry) =>
            Expression.Call(
                property,
                _contains,
                CreateAndConvertParameter<Geometry>(geometry));

        public static Expression Distance(Expression property, Expression geometry) =>
            Expression.Call(property, _distance, geometry);

        public static Expression Distance(Expression property, Geometry geometry) =>
            Expression.Call(
                property,
                _distance,
                CreateAndConvertParameter<Geometry>(geometry));

        public static Expression Intersects(Expression property, Expression geometry) =>
            Expression.Call(property, _intersects, geometry);

        public static Expression Intersects(Expression property, Geometry geometry) =>
            Expression.Call(
                property,
                _intersects,
                CreateAndConvertParameter<Geometry>(geometry));

        public static Expression Overlaps(Expression property, Expression geometry) =>
            Expression.Call(property, _overlaps, geometry);

        public static Expression Overlaps(Expression property, Geometry geometry) =>
            Expression.Call(
                property,
                _overlaps,
                CreateAndConvertParameter<Geometry>(geometry));

        public static Expression Touches(Expression property, Expression geometry) =>
            Expression.Call(property, _touches, geometry);

        public static Expression Touches(Expression property, Geometry geometry) =>
            Expression.Call(
                property,
                _touches,
                CreateAndConvertParameter<Geometry>(geometry));

        public static Expression Within(Expression property, Expression geometry) =>
            Expression.Call(property, _within, geometry);

        public static Expression Within(Expression property, Geometry geometry) =>
            Expression.Call(
                property,
                _within,
                CreateAndConvertParameter<Geometry>(geometry));

        private static Expression CreateAndConvertParameter<T>(object value)
        {
            Expression<Func<T>> lambda = () => (T)value;
            return lambda.Body;
        }
    }
}
