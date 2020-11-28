namespace HotChocolate.Data.Neo4J.Language
{
    public abstract class PropertyContainer : Named
    {
        public abstract Property Prop(string name);
    }
}