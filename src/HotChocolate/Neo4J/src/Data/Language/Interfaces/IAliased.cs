namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// <para>
    /// An interface for element with an alias. An alias has a subtle difference to a symbolic name in cypher.
    /// Nodes and relationships can have symbolic names which in turn can be aliased as well.
    /// </para>
    /// <para>
    /// Therefore, the Cypher generator needs both <see cref="INamed"/> and <see cref="IAliased"/>.
    /// </para>
    /// </summary>
    public interface IAliased
    {
        /// <summary>
        ///
        /// </summary>
        /// <returns>The alias.</returns>
        string GetAlias();

        /// <summary>
        /// Turns this alias into a symbolic name that can be used as an <see cref="Expression"/>.
        /// </summary>
        /// <returns>A new symbolic name.</returns>
        SymbolicName AsName();
    }
}
