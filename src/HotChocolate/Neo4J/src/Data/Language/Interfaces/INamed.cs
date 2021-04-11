#nullable enable

namespace HotChocolate.Data.Neo4J.Language
{
    // A named element which can have a symbolic name.
    public interface INamed
    {
        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public SymbolicName? GetSymbolicName();

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public SymbolicName GetRequiredSymbolicName();
    }
}
