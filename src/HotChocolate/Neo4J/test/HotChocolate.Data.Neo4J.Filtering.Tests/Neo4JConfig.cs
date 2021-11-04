using Squadron;

namespace HotChocolate.Data.Neo4J.Filtering
{
    public class Neo4JConfig : Neo4jDefaultOptions
    {
        public override void Configure(ContainerResourceBuilder builder)
        {
            builder
                .Name("neo4j")
                .Image("neo4j:4.3.4")
                .InternalPort(7687)
                .AddEnvironmentVariable("NEO4J_AUTH=none")
                .WaitTimeout(180);
        }
    }
}
