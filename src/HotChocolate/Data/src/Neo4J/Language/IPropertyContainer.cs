namespace HotChocolate.Data.Neo4J.Language
{
    public interface IPropertyContainer : INamed
    {
        public abstract Property Property(string name);
    }
}
