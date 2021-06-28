using System;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Filters
{
    /// <summary>
    /// This filter operation handler maps a NotLowerThanOrEquals operation field to a
    /// <see cref="FilterDefinition{TDocument}"/>
    /// </summary>
    public class MongoDbComparableNotLowerThanOrEqualsHandler
        : MongoDbComparableOperationHandler
    {
        public MongoDbComparableNotLowerThanOrEqualsHandler()
        {
            CanBeNull = false;
        }

        /// <inheritdoc />
        protected override int Operation => DefaultFilterOperations.NotLowerThanOrEquals;

        /// <inheritdoc />
        public override MongoDbFilterDefinition HandleOperation(
            MongoDbFilterVisitorContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue)
        {
            if (parsedValue is {})
            {
                var doc = new NotMongoDbFilterDefinition(
                    new MongoDbFilterOperation("$lte", parsedValue));

                return new MongoDbFilterOperation(context.GetMongoFilterScope().GetPath(), doc);
            }

            throw new InvalidOperationException();
        }
    }
}
