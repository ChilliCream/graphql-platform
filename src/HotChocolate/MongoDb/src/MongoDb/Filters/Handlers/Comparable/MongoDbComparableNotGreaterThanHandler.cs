using System;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Data.Filters
{
    public class MongoDbComparableNotGreaterThanHandler
        : MongoDbComparableOperationHandler
    {
        public MongoDbComparableNotGreaterThanHandler()
        {
            CanBeNull = false;
        }

        protected override int Operation => DefaultFilterOperations.NotGreaterThan;

        public override MongoDbFilterDefinition HandleOperation(
            MongoDbFilterVisitorContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue)
        {
            if (parsedValue is {})
            {
                var doc = new NotMongoDbFilterDefinition(
                    new MongoDbFilterOperation("$gt", parsedValue));

                return new MongoDbFilterOperation(context.GetMongoFilterScope().GetPath(), doc);
            }

            throw new InvalidOperationException();
        }
    }
}
