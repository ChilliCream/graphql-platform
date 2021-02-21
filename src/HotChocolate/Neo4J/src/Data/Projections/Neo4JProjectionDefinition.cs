namespace HotChocolate.Data.Neo4J.Projections
{
    public class Neo4JProjectionDefinition
    {
        public string __empty { get; set; } = string.Empty;

        private class ProjectionDefinitionWrapper
        {
            private readonly Neo4JProjectionDefinition _projection;

            public ProjectionDefinitionWrapper(Neo4JProjectionDefinition projection)
            {
                _projection = projection;
            }
        }
    }
}
