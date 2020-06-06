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
        private static readonly MethodInfo _area =
            typeof(Geometry).GetMethods().Single(m =>
                m.Name.Equals(nameof(Geometry.Area)));

        public static bool Area(
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

            result = Expression.Empty();

            return true;
        }
    }
}
