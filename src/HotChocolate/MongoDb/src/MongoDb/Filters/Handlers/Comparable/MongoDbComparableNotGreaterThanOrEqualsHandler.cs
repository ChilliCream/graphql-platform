using System;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Data.Filters
{
    public class MongoDbComparableNotGreaterThanOrEqualsHandler
        : MongoDbComparableOperationHandler
    {
        public MongoDbComparableNotGreaterThanOrEqualsHandler()
        {
            CanBeNull = false;
        }

        protected override int Operation => DefaultFilterOperations.NotGreaterThanOrEquals;

        public override MongoDbFilterDefinition HandleOperation(
            MongoDbFilterVisitorContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue)
        {
            if (parsedValue is {})
            {
                var doc = new NotMongoDbFilterDefinition(
                    new MongoDbFilterOperation("$gte", parsedValue));

                return new MongoDbFilterOperation(context.GetMongoFilterScope().GetPath(), doc);
            }

            throw new InvalidOperationException();
        }
    }
}
