namespace HotChocolate.Data.Neo4J.Language
{
    public interface IRelationshipPattern : IPatternElement, IExposesRelationships<RelationshipChain>
    {
        IExposesRelationships<RelationshipChain> Named(string name);
    }
}
