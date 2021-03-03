#nullable enable

namespace HotChocolate.Data.Neo4J.Language
{
    // A named element which can have a symbolic name.
    public interface INamed
    {
        public SymbolicName? GetSymbolicName();

        public SymbolicName GetRequiredSymbolicName();
    }
}
