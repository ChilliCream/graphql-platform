namespace HotChocolate.Data.Neo4J.Language
{
    public interface IVisitable
    {
        /// <summary>
        /// Returns the <see cref="ClauseKind"/> of the vistable.
        /// </summary>
        ClauseKind Kind { get; }

        /// <summary>
        /// Visits a visitor
        /// </summary>
        /// <param name="visitor">The visitor to notify, must not be null.</param>
        void Visit(CypherVisitor visitor);
    }
}
