using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using HotChocolate.Language;
using HotChocolate.Types.Filters.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace HotChocolate.Types.Filters.Mongo
{
    public static partial class StringOperationHandlers
    {
        public static bool Contains(
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

            if (operation.Type == typeof(string) &&
                type.IsInstanceOfType(value) &&
                parsedValue is string str &&
                context is MongoFilterVisitorContext ctx)
            {
                result = Query.Matches(
                    ctx.GetMongoFilterScope().GetPath(),
                    new BsonRegularExpression($"/{Regex.Escape(str)}/"));

                return true;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public static bool NotContains(
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

            if (operation.Type == typeof(string) &&
                type.IsInstanceOfType(value) &&
                parsedValue is string str &&
                context is MongoFilterVisitorContext ctx)
            {
                result = Query.Not(
                    Query.Matches(
                        ctx.GetMongoFilterScope().GetPath(),
                        new BsonRegularExpression($"/{Regex.Escape(str)}/")));

                return true;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
