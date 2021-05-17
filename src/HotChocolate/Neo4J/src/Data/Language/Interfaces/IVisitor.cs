namespace HotChocolate.Data.Neo4J.Language
{
    public interface IVisitor
    {
        void Enter(IVisitable visitable);

        void Leave(IVisitable visitable);
    }
}
