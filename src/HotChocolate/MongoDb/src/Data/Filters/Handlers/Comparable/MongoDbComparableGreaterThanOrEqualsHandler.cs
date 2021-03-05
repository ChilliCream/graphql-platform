using System;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Filters
{
    /// <summary>
    /// This filter operation handler maps a GreaterThanOrEquals operation field to a
    /// <see cref="FilterDefinition{TDocument}"/>
    /// </summary>
    public class MongoDbComparableGreaterThanOrEqualsHandler
        : MongoDbComparableOperationHandler
    {
        public MongoDbComparableGreaterThanOrEqualsHandler()
        {
            CanBeNull = false;
        }

        /// <inheritdoc />
        protected override int Operation => DefaultFilterOperations.GreaterThanOrEquals;

        /// <inheritdoc />
        public override MongoDbFilterDefinition HandleOperation(
            MongoDbFilterVisitorContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue)
        {
            if (parsedValue is {})
            {
                var doc = new MongoDbFilterOperation("$gte", parsedValue);
                return new MongoDbFilterOperation(context.GetMongoFilterScope().GetPath(), doc);
            }

            throw new InvalidOperationException();
        }
    }
}
