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

            if (operation.Kind == FilterOperationKind.ArrayAny &&
                type.IsInstanceOfType(value) &&
                parsedValue is bool parsedBool &&
                context is MongoFilterVisitorContext ctx)
            {
                var path = ctx.GetMongoFilterScope().GetPath(field);

                if (parsedBool)
                {
                    result = new BsonDocument(path,
                        new BsonDocument {
                            {"$exists", true },
                            {"$nin", new BsonArray(){ new BsonArray(), BsonNull.Value } }
                        });
                }
                else
                {
                    result = new BsonDocument(path,
                        new BsonDocument {
                            {"$exists", true},
                            {"$ne", BsonNull.Value},
                            {"$eq", new BsonArray() }
                        });
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
