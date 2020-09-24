using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NetTopologySuite.Geometries;

namespace HotChocolate.Data.Spatial.Filters
{
    internal static class ExpressionBuilder
    {
        private static readonly MethodInfo _contains =
            typeof(Geometry).GetMethod(nameof(Geometry.Contains))!;
        private static readonly MethodInfo _distance =
            typeof(Geometry).GetMethod(nameof(Geometry.Distance))!;

        private static readonly MethodInfo _buffer =
            typeof(Geometry)
                .GetMethods()
                .Single(
                    m => m.Name.Equals(nameof(Geometry.Buffer)) &&
                        m.GetParameters().Length == 1);

        public static Expression Buffer(Geometry geometry, double buffer)
        {
            return Expression.Call(
                CreateAndConvertParameter<Geometry>(geometry),
                _buffer,
                CreateAndConvertParameter<double>(buffer));
        }

        public static Expression Contains(Expression property, Expression geometry)
        {
            return Expression.Call(property, _contains, geometry);
        }

        public static Expression Contains(Expression property, Geometry geometry)
        {
            return Expression.Call(
                property,
                _contains,
                CreateAndConvertParameter<Geometry>(geometry));
        }

        public static Expression Distance(Expression property, Expression geometry)
        {
            return Expression.Call(property, _distance, geometry);
        }

        public static Expression Distance(Expression property, Geometry geometry)
        {
            return Expression.Call(
                property,
                _distance,
                CreateAndConvertParameter<Geometry>(geometry));
        }

        private static Expression CreateAndConvertParameter<T>(object value)
        {
            Expression<Func<T>> lambda = () => (T)value;
            return lambda.Body;
        }
    }
}
