using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace HotChocolate.Data.MongoDb
{
    /// <summary>
    /// This class was ported over from the official mongo db driver
    /// </summary>
    internal sealed class MongoDbCombinedProjectionDefinition : MongoDbProjectionDefinition
    {
        private readonly MongoDbProjectionDefinition[] _projections;

        public MongoDbCombinedProjectionDefinition(params MongoDbProjectionDefinition[] projections)
        {
            _projections = projections;
        }

        public MongoDbCombinedProjectionDefinition(
            IEnumerable<MongoDbProjectionDefinition> projections)
        {
            _projections = Ensure.IsNotNull(projections, nameof(projections)).ToArray();
        }

        public override BsonDocument Render(
            IBsonSerializer documentSerializer,
            IBsonSerializerRegistry serializerRegistry)
        {
            var document = new BsonDocument();

            foreach (var sort in _projections)
            {
                BsonDocument renderedProjection = sort.Render(
                    documentSerializer,
                    serializerRegistry);

                foreach (BsonElement element in renderedProjection.Elements)
                {
                    document.Remove(element.Name);
                    document.Add(element);
                }
            }

            return document;
        }
    }
}
