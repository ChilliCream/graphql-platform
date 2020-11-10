using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace HotChocolate.MongoDb.Data
{
    /// <summary>
    /// This class was ported over from the official mongo db driver
    /// </summary>
    internal sealed class MongoDbCombinedSortDefinition : MongoDbSortDefinition
    {
        private readonly MongoDbSortDefinition[] _sorts;

        public MongoDbCombinedSortDefinition(params MongoDbSortDefinition[] sorts)
        {
            _sorts = sorts;
        }

        public MongoDbCombinedSortDefinition(IEnumerable<MongoDbSortDefinition> sorts)
        {
            _sorts = Ensure.IsNotNull(sorts, nameof(sorts)).ToArray();
        }

        public override BsonDocument Render(
            IBsonSerializer documentSerializer,
            IBsonSerializerRegistry serializerRegistry)
        {
            var document = new BsonDocument();

            foreach (var sort in _sorts)
            {
                BsonDocument renderedSort = sort.Render(documentSerializer, serializerRegistry);

                foreach (BsonElement element in renderedSort.Elements)
                {
                    // the last sort always wins, and we need to make sure that order is preserved.
                    document.Remove(element.Name);
                    document.Add(element);
                }
            }

            return document;
        }
    }
}
