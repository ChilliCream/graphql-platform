namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// <para>
    /// An interface for element with an alias. An alias has a subtle difference to a symbolic name in cypher. Nodes and relationships can
    /// have symbolic names which in turn can be aliased as well.
    /// </para>
    /// <para>Therefore, the Cypher generator needs both {INamed} and {IAliased}.</para>
    /// </summary>
    public interface IAliased
    {
        string GetAlias();
        SymbolicName AsName();
    }
}
