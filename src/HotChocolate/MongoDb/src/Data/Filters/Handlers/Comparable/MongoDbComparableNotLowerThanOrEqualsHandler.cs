using System;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Filters
{
    public class MongoDbComparableNotLowerThanOrEqualsHandler
        : MongoDbComparableOperationHandler
    {
        public MongoDbComparableNotLowerThanOrEqualsHandler()
        {
            CanBeNull = false;
        }

        protected override int Operation => DefaultFilterOperations.NotLowerThanOrEquals;

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
