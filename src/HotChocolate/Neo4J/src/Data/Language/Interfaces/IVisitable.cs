namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// The visitable interface declares a set of visiting methods the correspond to the class.
    /// </summary>
    public interface IVisitable
    {
        /// <summary>
        /// Returns the <see cref="ClauseKind"/> of the visitable.
        /// </summary>
        ClauseKind Kind { get; }

        /// <summary>
        /// Visits a visitor
        /// </summary>
        /// <param name="cypherVisitor">The visitor to notify, must not be null.</param>
        public void Visit(CypherVisitor cypherVisitor);
    }
}
