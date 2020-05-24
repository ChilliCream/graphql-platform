using System;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Types.Filters.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.Types.Filters.Mongo
{
    public static class BooleanOperationHandlers
    {
        public static bool Equals(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            FilterOperationField field,
            IFilterVisitorContext<FilterDefinition<BsonDocument>> context,
            [NotNullWhen(true)]out FilterDefinition<BsonDocument>? result)
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
                FilterDefinition<BsonDocument> property = context.GetInstance();

                if (!operation.IsSimpleArrayType())
                {
                    //
                }

                result = ctx.Builder.Eq(
                    ctx.GetMongoFilterScope().GetPath(field), valueOfT);

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
            FilterOperationField field,
            IFilterVisitorContext<FilterDefinition<BsonDocument>> context,

            [NotNullWhen(true)]out FilterDefinition<BsonDocument>? result)
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
                FilterDefinition<BsonDocument> property = context.GetInstance();

                if (!operation.IsSimpleArrayType())
                {
                    //
                }

                result = ctx.Builder.Ne(
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
