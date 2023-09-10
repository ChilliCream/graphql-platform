namespace HotChocolate.Data.Neo4J.Integration.AnnotationBased.Models;

[Neo4JNode("Actor")]
public class Actor
{
    public string Name { get; set; } = default!;

    [Neo4JRelationship("ACTED_IN")]
    public List<Movie> ActedIn { get; set; } = default!;
}
