namespace HotChocolate.Data.Neo4J.Language
{
    public interface INamed
    {
        public abstract SymbolicName GetSymbolicName();
        public SymbolicName GetRequiredSymbolicName();
    }
}