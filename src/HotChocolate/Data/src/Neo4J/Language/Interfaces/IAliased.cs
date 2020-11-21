namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// <para>
    /// An interface for element with an alias. An alias has a subtle difference to a symbolic name in cypher. Nodes and relationships can
    /// have symbolic names which in turn can be aliased as well.
    /// </para>
    /// <para>Therefor, the Cypher generator needs both {Named} and {Aliased}.</para>
    /// </summary>
    public interface IAliased
    {
        string GetAlias();
        SymbolicName AsName();
    }
}