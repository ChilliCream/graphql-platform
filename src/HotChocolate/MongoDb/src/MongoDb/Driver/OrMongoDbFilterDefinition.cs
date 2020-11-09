using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace HotChocolate.MongoDb.Data
{
    /// <summary>
    /// This class was ported over from the official mongo db driver
    /// </summary>
    internal sealed class OrMongoDbFilterDefinition : MongoDbFilterDefinition
    {
        private readonly List<MongoDbFilterDefinition> _filters;

        public OrMongoDbFilterDefinition(IEnumerable<MongoDbFilterDefinition> filters)
        {
            _filters = filters.ToList();
        }

        public override BsonDocument Render(
            IBsonSerializer documentSerializer,
            IBsonSerializerRegistry serializerRegistry)
        {
            var clauses = new BsonArray();

            foreach (var filter in _filters)
            {
                BsonDocument renderedFilter = filter.Render(documentSerializer, serializerRegistry);
                AddClause(clauses, renderedFilter);
            }

            return new BsonDocument("$or", clauses);
        }

        private static void AddClause(BsonArray clauses, BsonDocument filter)
        {
            if (filter.ElementCount == 1 && filter.GetElement(0).Name == "$or")
            {
                // flatten nested $or
                clauses.AddRange((BsonArray)filter[0]);
            }
            else
            {
                // we could shortcut the user's query if there are no elements in the filter, but
                // I'd rather be literal and let them discover the problem on their own.
                clauses.Add(filter);
            }
        }
    }
}
