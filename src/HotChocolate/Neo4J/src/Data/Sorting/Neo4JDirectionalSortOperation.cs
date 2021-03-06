using HotChocolate.Data.Neo4J.Language;

namespace HotChocolate.Data.Neo4J.Sorting
{
    internal sealed class Neo4JDirectionalSortOperation : Neo4JSortDefinition
    {
        private readonly string _path;
        private readonly SortDirection _direction;

        public Neo4JDirectionalSortOperation(
            string field,
            SortDirection direction)
        {
            _path = Ensure.IsNotNull(field, nameof(field));
            _direction = direction;
        }
    }
}
