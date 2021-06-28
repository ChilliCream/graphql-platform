using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace HotChocolate.Data.MongoDb
{
    /// <summary>
    /// This class was ported over from the official mongo db driver
    /// </summary>
    internal sealed class NotMongoDbFilterDefinition : MongoDbFilterDefinition
    {
        private readonly MongoDbFilterDefinition _filter;

        public NotMongoDbFilterDefinition(MongoDbFilterDefinition filter)
        {
            _filter = filter;
        }

        public override BsonDocument Render(
            IBsonSerializer documentSerializer,
            IBsonSerializerRegistry serializerRegistry)
        {
            var renderedFilter = _filter.Render(documentSerializer, serializerRegistry);

            if (renderedFilter.ElementCount == 1)
            {
                return NegateSingleElementFilter(renderedFilter, renderedFilter.GetElement(0));
            }

            return NegateArbitraryFilter(renderedFilter);
        }

        private static BsonDocument NegateArbitraryFilter(BsonDocument filter)
        {
            if (filter.ElementCount == 1 &&
                filter.GetElement(0).Name.StartsWith("$", StringComparison.Ordinal))
            {
                return new BsonDocument("$not", filter);
            }
            else
            {
                // $not only works as a meta operator on a single operator so simulate Not using $nor
                return new BsonDocument("$nor", new BsonArray { filter });
            }
        }

        private static BsonDocument NegateSingleElementFilter(
            BsonDocument filter,
            BsonElement element)
        {
            if (element.Name[0] == '$')
            {
                return NegateSingleElementTopLevelOperatorFilter(filter, element);
            }

            if (element.Value is BsonDocument)
            {
                var selector = (BsonDocument)element.Value;
                if (selector.ElementCount >= 1)
                {
                    var operatorName = selector.GetElement(0).Name;
                    if (operatorName[0] == '$' && operatorName != "$ref")
                    {
                        if (selector.ElementCount == 1)
                        {
                            return NegateSingleFieldOperatorFilter(
                                element.Name,
                                selector.GetElement(0));
                        }

                        return NegateArbitraryFilter(filter);
                    }
                }
            }

            if (element.Value is BsonRegularExpression)
            {
                return new BsonDocument(element.Name, new BsonDocument("$not", element.Value));
            }

            return new BsonDocument(element.Name, new BsonDocument("$ne", element.Value));
        }

        private static BsonDocument NegateSingleFieldOperatorFilter(
            string field,
            BsonElement element)
        {
            switch (element.Name)
            {
                case "$exists":
                    return new BsonDocument(
                        field,
                        new BsonDocument("$exists", !element.Value.ToBoolean()));
                case "$in":
                    return new BsonDocument(
                        field,
                        new BsonDocument("$nin", (BsonArray)element.Value));
                case "$ne":
                case "$not":
                    return new BsonDocument(field, element.Value);
                case "$nin":
                    return new BsonDocument(
                        field,
                        new BsonDocument("$in", (BsonArray)element.Value));
                default:
                    return new BsonDocument(
                        field,
                        new BsonDocument("$not", new BsonDocument(element)));
            }
        }

        private static BsonDocument NegateSingleElementTopLevelOperatorFilter(
            BsonDocument filter,
            BsonElement element)
        {
            switch (element.Name)
            {
                case "$and":
                    return new BsonDocument("$nor", new BsonArray { filter });
                case "$or":
                    return new BsonDocument("$nor", element.Value);
                case "$nor":
                    return new BsonDocument("$or", element.Value);
                default:
                    return NegateArbitraryFilter(filter);
            }
        }
    }
}
