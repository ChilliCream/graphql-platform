using System;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Types.Filters.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.Types.Filters.Mongo
{
    public static class ArrayOperationHandler
    {
        public static bool ArrayAny(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            IFilterVisitorContext<FilterDefinition<BsonDocument>> context,
            [NotNullWhen(true)] out FilterDefinition<BsonDocument>? result)
        {
            object parsedValue = type.ParseLiteral(value);

            if (parsedValue == null)
            {
                context.ReportError(
                    ErrorHelper.CreateNonNullError(operation, type, value, context));

                result = null;
                return false;
            }

            if (operation.Kind == FilterOperationKind.ArrayAny &&
                type.IsInstanceOfType(value) &&
                parsedValue is bool parsedBool &&
                context is MongoFilterVisitorContext ctx)
            {
                if (parsedBool)
                {
                    result = ctx.Builder.SizeGt(
                        ctx.GetMongoFilterScope().GetPath(), 0);
                }
                else
                {
                    result = ctx.Builder.Size(
                        ctx.GetMongoFilterScope().GetPath(), 0);
                }

                return true;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
