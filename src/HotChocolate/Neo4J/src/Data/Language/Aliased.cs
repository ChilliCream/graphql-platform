namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// An element with an alias. An alias has a subtle difference to a symbolic name in cypher. Nodes and relationships can
    /// have symbolic names which in turn can be aliased as well.
    /// Therefore, the Cypher generator needs both {Named} and {Aliased}.
    /// </summary>
    public abstract class Aliased : IAliased
    {
        /// <summary>
        /// Turns this alias into a symbolic name that can be used as an Expression.
        /// </summary>
        /// <returns>A new symbolic name</returns>
        public SymbolicName AsName() => SymbolicName.Of(GetAlias());
        public abstract string GetAlias();
    }
}
