namespace HotChocolate.Data.Neo4J.Integration.AnnotationBased.Models;

[Neo4JNode("Movie")]
public class Movie
{
    public string Title { get; set; } = default!;

    [Neo4JRelationship("ACTED_IN", RelationshipDirection.Incoming)]
    public List<Actor> Actors { get; set; } = default!;
}
