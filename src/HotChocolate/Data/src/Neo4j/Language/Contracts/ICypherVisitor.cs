namespace HotChocolate.Data.Neo4j
{
    public interface ICypherVisitor
    {
        public void Enter(IVisitable visitable);
        public void Leave(IVisitable visitable);
    }
}
