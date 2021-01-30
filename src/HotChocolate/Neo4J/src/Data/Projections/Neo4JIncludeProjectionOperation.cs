using HotChocolate.Data.Neo4J.Language;

namespace HotChocolate.Data.Neo4J.Projections
{
    internal sealed class Neo4JIncludeProjectionOperation : Neo4JProjectionDefinition
    {
        private readonly string _path;

        public Neo4JIncludeProjectionOperation (
            string field)
        {
            //_path = Ensure.IsNotNull(field, nameof(field));
        }
    }
}
