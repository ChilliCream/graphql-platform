using System;
using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Filters
{
    public class MongoDbComparableGreaterThanHandler
        : MongoDbComparableOperationHandler
    {
        public MongoDbComparableGreaterThanHandler()
        {
            CanBeNull = false;
        }

        protected override int Operation => DefaultFilterOperations.GreaterThan;

        public override MongoDbFilterDefinition HandleOperation(
            MongoDbFilterVisitorContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue)
        {
            if (parsedValue is {})
            {
                var doc = new MongoDbFilterOperation("$gt", parsedValue);
                return new MongoDbFilterOperation(context.GetMongoFilterScope().GetPath(), doc);
            }

            throw new InvalidOperationException();
        }
    }
}
