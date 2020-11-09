using System;
using System.Text.RegularExpressions;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Data.Filters
{
    public class MongoDbStringNotContainsHandler
        : MongoDbStringOperationHandler
    {
        public MongoDbStringNotContainsHandler()
        {
            CanBeNull = false;
        }

        protected override int Operation => DefaultOperations.NotContains;

        public override MongoDbFilterDefinition HandleOperation(
            MongoDbFilterVisitorContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue)
        {
            if (parsedValue is string str)
            {
                var doc = new NotMongoDbFilterDefinition(
                    new MongoDbFilterOperation(
                        "$regex",
                        new BsonRegularExpression($"/{Regex.Escape(str)}/")));

                return new MongoDbFilterOperation(context.GetMongoFilterScope().GetPath(), doc);
            }

            throw new InvalidOperationException();
        }
    }
}
