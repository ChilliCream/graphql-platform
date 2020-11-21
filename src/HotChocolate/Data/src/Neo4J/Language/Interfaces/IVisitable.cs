namespace HotChocolate.Data.Neo4J.Language
{
    public interface IVisitable
    {
        public ClauseKind Kind { get; }

        /// <summary>
        /// Visits a visitor
        /// </summary>
        /// <param name="visitor">The visitor to notify, must not be null.</param>
        public void Visit(CypherVisitor visitor);
    }
}