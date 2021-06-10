namespace HotChocolate.Data.Neo4J.Integration.SchemaFirst
{
    [global::HotChocolate.Types.ExtendObjectType("Query")]
    public partial class Query
    {
        [global::HotChocolate.GraphQLNameAttribute("movies")]
        [global::HotChocolate.Data.Neo4J.UseNeo4JDatabaseAttribute(databaseName: "neo4j")]
        [global::HotChocolate.Data.UseProjectionAttribute]
        [global::HotChocolate.Data.UseFilteringAttribute]
        [global::HotChocolate.Data.UseSortingAttribute]
        public global::HotChocolate.Data.Neo4J.Execution.Neo4JExecutable<Movie> GetMovies([global::HotChocolate.ScopedServiceAttribute] global::Neo4j.Driver.IAsyncSession session) => new(session);
        [global::HotChocolate.GraphQLNameAttribute("actors")]
        [global::HotChocolate.Data.Neo4J.UseNeo4JDatabaseAttribute(databaseName: "neo4j")]
        [global::HotChocolate.Data.UseProjectionAttribute]
        [global::HotChocolate.Data.UseFilteringAttribute]
        [global::HotChocolate.Data.UseSortingAttribute]
        public global::HotChocolate.Data.Neo4J.Execution.Neo4JExecutable<Actor> GetActors([global::HotChocolate.ScopedServiceAttribute] global::Neo4j.Driver.IAsyncSession session) => new(session);
    }
}