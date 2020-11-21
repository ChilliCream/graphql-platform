namespace HotChocolate.Data.Neo4J.Language
{
    public abstract class Visitable : IVisitable
    {
        public ClauseKind Kind => ClauseKind.Default;
        /// <summary>
        /// Visits a Visitor visiting this Visitable and its nested Visitables if applicable.
        /// </summary>
        /// <param name="visitor"></param>
        public void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            visitor.Leave(this);
        }

        /// <summary>
        /// A helper method that presents the visitor to the visitable if the visitable is not null.
        /// Not meant to be overridden.
        /// </summary>
        /// <param name="visitable">The visitable to visit if not null</param>
        /// <param name="visitor">The visitor to use</param>
        public static void VisitIfNotNull(IVisitable visitable, CypherVisitor visitor) => visitable?.Visit(visitor);
    }
}