using System;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Types.Filters.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.Types.Filters.Mongo
{
    public static partial class MongoOperationHandlers
    {
        public static bool In(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            FilterOperationField field,
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

            if (type.IsInstanceOfType(value) &&
                context is MongoFilterVisitorContext ctx)
            {

                if (operation.IsSimpleArrayType())
                {
                    result = new BsonDocument { { "$in", BsonArray.Create(parsedValue) } };
                }
                else
                {
                    result = ctx.Builder.In(
                        ctx.GetMongoFilterScope().GetPath(field),
                        BsonArray.Create(parsedValue));
                }

                return true;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public static bool NotIn(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            FilterOperationField field,
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

            if (type.IsInstanceOfType(value) &&
                context is MongoFilterVisitorContext ctx)
            {


                if (operation.IsSimpleArrayType())
                {
                    result = new BsonDocument { { "$nin", BsonArray.Create(parsedValue) } };
                }
                else
                {
                    result = ctx.Builder.Nin(
                        ctx.GetMongoFilterScope().GetPath(field),
                        BsonArray.Create(parsedValue));
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
