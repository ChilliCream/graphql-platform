namespace HotChocolate.Data.Neo4J.Filtering
{
    public class Neo4JFilterOperation : Neo4JFilterDefinition
    {
        private readonly string _path;
        private readonly object? _value;

        public Neo4JFilterOperation(string path, object? value)
        {
            _path = path;
            _value = value;
        }

        // TODO: Implement Filter operation
    }
}
