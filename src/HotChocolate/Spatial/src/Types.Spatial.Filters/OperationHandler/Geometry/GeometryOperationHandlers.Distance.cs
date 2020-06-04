using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Filters.Expressions
{
    public static partial class GeometryOperationHandlers
    {
        private static readonly MethodInfo _distance =
            typeof(Geometry).GetMethods().Single(m =>
                m.Name.Equals(nameof(Geometry.Distance))
                && m.GetParameters().Length == 1
                && m.GetParameters().Single().ParameterType == typeof(Geometry));

        public static bool Distance(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            FilterOperationField _,
            IFilterVisitorContext<Expression> context,
            [NotNullWhen(true)] out Expression result)
        {
            object parsedValue = type.ParseLiteral(value);

            if (parsedValue == null)
            {
                context.ReportError(
                    ErrorHelper.CreateNonNullError(operation, type, value, context));

                result = null!;
                return false;
            }

            if (type.IsInstanceOfType(value) &&
                parsedValue is bool)
            {
                Expression property = context.GetInstance();

                if (!operation.IsSimpleArrayType())
                {
                    property = Expression.Property(context.GetInstance(), operation.Property);
                }

                result = Expression.AndAlso(
                    Expression.NotEqual(property, Expression.Constant(null)),
                    Expression.Call(property, _distance, Expression.Constant(value)));

                return true;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
