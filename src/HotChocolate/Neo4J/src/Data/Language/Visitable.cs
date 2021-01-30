namespace HotChocolate.Data.Neo4J.Language
{
    public abstract class Visitable : IVisitable
    {
        public abstract ClauseKind Kind { get; }
        /// <summary>
        /// Visits a Visitor visiting this Visitable and its nested Visitables if applicable.
        /// </summary>
        /// <param name="visitor"></param>
        public virtual void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            visitor.Leave(this);
        }
    }
}
