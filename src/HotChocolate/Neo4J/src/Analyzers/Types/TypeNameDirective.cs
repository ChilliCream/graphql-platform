namespace HotChocolate.Data.Neo4J.Analyzers.Types
{
    public class TypeNameDirective
    {
        public TypeNameDirective(string name, string pluralName)
        {
            Name = name;
            PluralName = pluralName;
        }

        public string Name { get; }

        public string PluralName { get; }
    }
}
