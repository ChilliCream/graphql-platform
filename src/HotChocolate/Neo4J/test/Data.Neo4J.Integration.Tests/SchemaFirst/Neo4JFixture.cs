using HotChocolate.Data.Neo4J.Testing;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Neo4J.Integration.SchemaFirst;

public class Neo4JFixture : Neo4JFixtureBase
{
    private const string SeedCypher = @"
        CREATE (TheMatrix:Movie {Title:'The Matrix', Released:1999, Tagline:'Welcome to the Real World'})
        CREATE (Keanu:Actor {Name:'Keanu Reeves', Born:1964})
        CREATE (Carrie:Actor {Name:'Carrie-Anne Moss', Born:1967})
        CREATE (Laurence:Actor {Name:'Laurence Fishburne', Born:1961})
        CREATE (Hugo:Actor {Name:'Hugo Weaving', Born:1960})
        CREATE (LillyW:Actor {Name:'Lilly Wachowski', Born:1967})
        CREATE (LanaW:Actor {Name:'Lana Wachowski', Born:1965})
        CREATE (JoelS:Actor {Name:'Joel Silver', Born:1952})
        CREATE
          (Keanu)-[:ACTED_IN {Roles:['Neo']}]->(TheMatrix),
          (Carrie)-[:ACTED_IN {Roles:['Trinity']}]->(TheMatrix),
          (Laurence)-[:ACTED_IN {Roles:['Morpheus']}]->(TheMatrix),
          (Hugo)-[:ACTED_IN {Roles:['Agent Smith']}]->(TheMatrix),
          (LillyW)-[:DIRECTED]->(TheMatrix),
          (LanaW)-[:DIRECTED]->(TheMatrix),
          (JoelS)-[:PRODUCED]->(TheMatrix)

        CREATE (Emil:Actor {Name: 'Emil Eifrem', Born:1978})
        CREATE (Emil)-[:ACTED_IN {Roles:['Emil']}]->(TheMatrix)

        CREATE (TheMatrixReloaded:Movie {Title:'The Matrix Reloaded', Released:2003, Tagline:'Free your mind'})
        CREATE
          (Keanu)-[:ACTED_IN {Roles:['Neo']}]->(TheMatrixReloaded),
          (Carrie)-[:ACTED_IN {Roles:['Trinity']}]->(TheMatrixReloaded),
          (Laurence)-[:ACTED_IN {Roles:['Morpheus']}]->(TheMatrixReloaded),
          (Hugo)-[:ACTED_IN {Roles:['Agent Smith']}]->(TheMatrixReloaded),
          (LillyW)-[:DIRECTED]->(TheMatrixReloaded),
          (LanaW)-[:DIRECTED]->(TheMatrixReloaded),
          (JoelS)-[:PRODUCED]->(TheMatrixReloaded)

        CREATE (TheMatrixRevolutions:Movie {Title:'The Matrix Revolutions', Released:2003, Tagline:'Everything that has a beginning has an end'})
        CREATE
          (Keanu)-[:ACTED_IN {Roles:['Neo']}]->(TheMatrixRevolutions),
          (Carrie)-[:ACTED_IN {Roles:['Trinity']}]->(TheMatrixRevolutions),
          (Laurence)-[:ACTED_IN {Roles:['Morpheus']}]->(TheMatrixRevolutions),
          (Hugo)-[:ACTED_IN {Roles:['Agent Smith']}]->(TheMatrixRevolutions),
          (LillyW)-[:DIRECTED]->(TheMatrixRevolutions),
          (LanaW)-[:DIRECTED]->(TheMatrixRevolutions),
          (JoelS)-[:PRODUCED]->(TheMatrixRevolutions)";

    public async Task<IRequestExecutor> Arrange(Neo4JDatabase database)
    {
        await ResetDatabase(database, SeedCypher);

        return await new ServiceCollection()
            .AddSingleton(database.Driver)
            .AddGraphQL()
            .AddQueryType()
            .AddMovieTypes()
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IRequestExecutorResolver>()
            .GetRequestExecutorAsync();
    }
}
