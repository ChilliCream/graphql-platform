using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Filters.Expressions
{
    public static partial class GemetryHandlers
    {
        private static readonly MethodInfo _distance =
            typeof(Geometry).GetMethods().Single(m =>
                m.Name.Equals(nameof(Geometry.Distance))
                && m.GetParameters().Length == 1
                && m.GetParameters().Single().ParameterType == typeof(Geometry));
        public static bool Enter(
            FilterOperationField field,
            ObjectFieldNode node,
            IFilterVisitorContext<Expression> context,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            var type = field.Type;
            var value = node.Value;
            var operation = field.Operation;
            object parsedValue = type.ParseLiteral(value);

            if (parsedValue == null)
            {
                context.ReportError(
                    ErrorHelper.CreateNonNullError(operation, type, value, context));

                action = SyntaxVisitor.SkipAndLeave;
                return true;
            }

            if (type.IsInstanceOfType(value) &&
                parsedValue is Dictionary<string, object> data &&
                data.TryGetValue("from", out object? geometryData) &&
                geometryData is Geometry geometry &&
                context is QueryableFilterVisitorContext ctx)
            {

                MemberExpression nestedProperty = Expression.Property(
                    context.GetInstance(),
                    field.Operation.Property);
                ctx.PushInstance(
                    Expression.Call(nestedProperty, _distance, Expression.Constant(geometry)));
                action = SyntaxVisitor.Continue;
                return true;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public static void Leave(
            FilterOperationField field,
            ObjectFieldNode node,
            IFilterVisitorContext<Expression> context)
        {
            if (context is QueryableFilterVisitorContext ctx)
            {
                ctx.PopInstance();
            }
        }

        /*
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
        parsedValue is Dictionary<string, object> data &&
        data.TryGetValue("from", out object? geometryData) &&
        geometryData is Geometry geometry)
    {
        Expression property = context.GetInstance();

        if (context.TryGetOperation(
            field.Operation.FilterKind,
            field.Operation.Kind,
            out FilterOperationHandler<T>? handler) &&
            handler(field.Operation, field.Type,
                node.Value, field, context, out T expression))
        {
            context.GetLevel().Enqueue(expression);
        }

        if (!operation.IsSimpleArrayType())
        {
            property = Expression.Property(context.GetInstance(), operation.Property);
        }

        result = Expression.NotEqual(property, Expression.Constant(null));
        var distance = Expression.AndAlso(
            Expression.Call(property, _distance, Expression.Constant(geometry)));


        return true;
    }
    else
    {
        throw new InvalidOperationException();
    }
}
*/
    }
}
