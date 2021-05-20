namespace HotChocolate.Data.Neo4J.Language
{
    // A named element which can have a symbolic name.
    public interface INamed
    {
        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        SymbolicName? SymbolicName { get; }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        SymbolicName RequiredSymbolicName { get; }
    }
}
