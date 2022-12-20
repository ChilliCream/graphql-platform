using Squadron;

namespace HotChocolate.Data.Neo4J.Testing;

public class Neo4JConfig : Neo4jDefaultOptions
{
    public override void Configure(ContainerResourceBuilder builder)
    {
        builder
            .Name("neo4j")
            .Image("neo4j:latest")
            .InternalPort(7687)
            .AddEnvironmentVariable("NEO4J_AUTH=none")
            .WaitTimeout(120);
    }
}
