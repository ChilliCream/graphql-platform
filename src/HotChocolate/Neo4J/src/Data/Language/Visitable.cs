namespace HotChocolate.Data.Neo4J.Language
{
    public abstract class Visitable : IVisitable
    {
        public abstract ClauseKind Kind { get; }
        /// <summary>
        /// Visits a visitor visiting this Visitable and its nested Visitable if applicable.
        /// </summary>
        /// <param name="cypherVisitor"></param>
        public virtual void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            cypherVisitor.Leave(this);
        }
    }
}
