namespace HotChocolate.Data.Neo4J.Language
{
    public interface INamed
    {
        public SymbolicName? GetSymbolicName();
        //public SymbolicName GetRequiredSymbolicName();
    }
}
