namespace HotChocolate.Data.Neo4j
{
    public interface IVisitable
    {
        //public void VisitIfNotNull(IVisitable visitable, IVisitor visitor);
        public void Visit(CypherVisitor visitor);
    }
}
