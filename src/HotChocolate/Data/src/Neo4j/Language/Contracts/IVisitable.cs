namespace HotChocolate.Data.Neo4j
{
    public interface IVisitable
    {
        public void Visit(CypherVisitor visitor);
    }
}
