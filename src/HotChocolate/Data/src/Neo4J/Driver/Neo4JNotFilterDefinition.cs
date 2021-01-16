namespace HotChocolate.Data.Neo4J.Driver
{
    public class Neo4JNotFilterDefinition : Neo4JFilterDefinition
    {
        private readonly Neo4JFilterDefinition _filter;

        public Neo4JNotFilterDefinition(Neo4JFilterDefinition filter)
        {
            _filter = filter;
        }
    }
}
