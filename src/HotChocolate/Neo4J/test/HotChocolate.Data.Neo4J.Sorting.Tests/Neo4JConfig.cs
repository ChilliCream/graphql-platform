using Squadron;

namespace HotChocolate.Data.Neo4J.Sorting
{
    public class Neo4JConfig : Neo4jDefaultOptions
    {
        public override void Configure(ContainerResourceBuilder builder)
        {
            builder
                .Name("neo4j")
                .Image("neo4j:4.2.0")
                .InternalPort(7687)
                .AddEnvironmentVariable("NEO4J_AUTH=none")
                .WaitTimeout(120);
        }
    }
}
