namespace HotChocolate.Data.Neo4J.Language
{
    public class Parameter
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public Parameter(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public override string ToString()
        {
            return $"${Name}";
        }
    }
}
