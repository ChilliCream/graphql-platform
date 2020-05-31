using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using HotChocolate.Language;
using HotChocolate.Types.Filters.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.Types.Filters.Mongo
{
    public static partial class StringOperationHandlers
    {
        public static bool StartsWith(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            FilterOperationField field,
            IFilterVisitorContext<FilterDefinition<BsonDocument>> context,
            [NotNullWhen(true)]out FilterDefinition<BsonDocument> result)
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
                parsedValue is string str &&
                context is MongoFilterVisitorContext ctx)
            {
                var doc = new BsonDocument("$regex",
                    new BsonRegularExpression($"/^{Regex.Escape(str)}/"));

                if (!operation.IsSimpleArrayType())
                {
                    doc = new BsonDocument(ctx.GetMongoFilterScope().GetPath(field), doc);
                }

                result = doc;
                return true;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public static bool NotStartsWith(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            FilterOperationField field,
            IFilterVisitorContext<FilterDefinition<BsonDocument>> context,
            [NotNullWhen(true)]out FilterDefinition<BsonDocument> result)
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
                parsedValue is string str &&
                context is MongoFilterVisitorContext ctx)
            {
                var doc = new BsonDocument("$not",
                    new BsonDocument("$regex",
                        new BsonRegularExpression($"/^{Regex.Escape(str)}/")));

                if (!operation.IsSimpleArrayType())
                {
                    doc = new BsonDocument(ctx.GetMongoFilterScope().GetPath(field), doc);
                }

                result = doc;
                return true;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
