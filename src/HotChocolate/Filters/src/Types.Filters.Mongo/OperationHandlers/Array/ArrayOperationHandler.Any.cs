using System;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Types.Filters.Expressions;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace HotChocolate.Types.Filters.Mongo
{
    public static class ArrayOperationHandler
    {
        public static bool ArrayAny(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            IFilterVisitorContext<IMongoQuery> context,
            [NotNullWhen(true)] out IMongoQuery? result)
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
                context is MongoFilterVisitorContext monogContext)
            {
                if (parsedBool)
                {
                    result = Query.SizeGreaterThan(
                        monogContext.GetMongoFilterScope().GetPath(), 0);
                }
                else
                {
                    result = Query.Size(
                        monogContext.GetMongoFilterScope().GetPath(), 0);
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
