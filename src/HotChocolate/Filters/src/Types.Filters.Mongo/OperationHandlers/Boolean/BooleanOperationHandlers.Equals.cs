using System;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Types.Filters.Expressions;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace HotChocolate.Types.Filters.Mongo
{
    public static class BooleanOperationHandlers
    {
        public static bool Equals(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            IFilterVisitorContext<IMongoQuery> context,
            [NotNullWhen(true)]out IMongoQuery? result)
        {
            object parsedValue = type.ParseLiteral(value);

            if (parsedValue == null)
            {
                context.ReportError(
                    ErrorHelper.CreateNonNullError(operation, type, value, context));

                result = null;
                return false;
            }

            if (operation.Type == typeof(bool) &&
                type.IsInstanceOfType(value) &&
                parsedValue is bool valueOfT &&
                context is MongoFilterVisitorContext ctx)
            {
                IMongoQuery property = context.GetInstance();

                if (!operation.IsSimpleArrayType())
                {
                    //
                }

                result = Query.EQ(
                    ctx.GetMongoFilterScope().GetPath(), valueOfT);

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

            if (parsedValue == null)
            {
                context.ReportError(
                    ErrorHelper.CreateNonNullError(operation, type, value, context));

                result = null;
                return false;
            }

            if (operation.Type == typeof(bool) &&
                type.IsInstanceOfType(value) &&
                parsedValue is bool valueOfT &&
                context is MongoFilterVisitorContext ctx)
            {
                IMongoQuery property = context.GetInstance();

                if (!operation.IsSimpleArrayType())
                {
                    //
                }

                result = Query.NE(
                    ctx.GetMongoFilterScope().GetPath(), valueOfT);
                return true;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
