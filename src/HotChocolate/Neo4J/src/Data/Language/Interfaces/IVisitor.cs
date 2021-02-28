using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    public interface IVisitor
    {
        public void Enter(IVisitable visitable);
        public void Leave(IVisitable visitable);
    }
}
