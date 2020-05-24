using System;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Types.Filters.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.Types.Filters.Mongo
{
    public static partial class ComparableOperationHandlers
    {
        public static bool LowerThanOrEquals(
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

            if (operation.Type == typeof(IComparable) &&
                type.IsInstanceOfType(value) &&
                context is MongoFilterVisitorContext ctx)
            {
                parsedValue = ParseValue(parsedValue, operation, type, ctx);

                result = ctx.Builder.Lte(
                    ctx.GetMongoFilterScope().GetPath(field),
                    BsonValue.Create(parsedValue));

                return true;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public static bool NotLowerThanOrEquals(
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

            if (operation.Type == typeof(IComparable) &&
                type.IsInstanceOfType(value) &&
                context is MongoFilterVisitorContext ctx)
            {
                parsedValue = ParseValue(parsedValue, operation, type, ctx);

                result = ctx.Builder.Not(
                    ctx.Builder.Lte(
                        ctx.GetMongoFilterScope().GetPath(field),
                        BsonValue.Create(parsedValue)));

                return true;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
