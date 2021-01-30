namespace HotChocolate.Data.Neo4J.Language
{
    public interface IExposesRelationship<out T> where T: RelationshipPattern
    {
        T RelationshipTo(Node other, params string[] types);
        T RelationshipFrom(Node other, params string[] types);
        T RelationshipBetween(Node other, params string[] types);
    }
}
