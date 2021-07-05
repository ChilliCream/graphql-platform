namespace HotChocolate.Analyzers.Configuration
{
    public class GraphQLConfigExtensions
    {
        public Neo4JSettings? Neo4J { get; set; }

        public EFCoreSettings? EF { get; set; }
    }
}
