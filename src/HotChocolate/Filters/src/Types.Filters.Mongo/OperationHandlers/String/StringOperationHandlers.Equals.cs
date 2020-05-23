using System;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Types.Filters.Expressions;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace HotChocolate.Types.Filters.Mongo
{
    public static partial class StringOperationHandlers
    {
        public static bool Equals(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            IFilterVisitorContext<IMongoQuery> context,
            [NotNullWhen(true)]out IMongoQuery? result)
        {
            object parsedValue = type.ParseLiteral(value);

            if (!operation.IsNullable && parsedValue == null)
            {
                context.ReportError(
                    ErrorHelper.CreateNonNullError(operation, type, value, context));

                result = null;
                return false;
            }

            if (operation.Type == typeof(IComparable) &&
                type.IsInstanceOfType(value) &&
                parsedValue is string str &&
                context is MongoFilterVisitorContext ctx)
            {
                result = Query.EQ(
                    ctx.GetMongoFilterScope().GetPath(), str);

                return true;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public static bool NotEquals(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            IFilterVisitorContext<IMongoQuery> context,
            [NotNullWhen(true)]out IMongoQuery? result)
        {
            object parsedValue = type.ParseLiteral(value);

            if (!operation.IsNullable && parsedValue == null)
            {
                context.ReportError(
                    ErrorHelper.CreateNonNullError(operation, type, value, context));

                result = null;
                return false;
            }

            if (operation.Type == typeof(IComparable) &&
                type.IsInstanceOfType(value) &&
                parsedValue is string str &&
                context is MongoFilterVisitorContext ctx)
            {
                result = Query.NE(
                    ctx.GetMongoFilterScope().GetPath(), str);

                return true;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
