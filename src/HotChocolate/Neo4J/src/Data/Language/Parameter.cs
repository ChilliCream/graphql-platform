namespace HotChocolate.Data.Neo4J.Language
{
    public class Parameter<T> : Expression
    {
        public Parameter(string name, T value)
        {
            Name = name;
            Value = value;
        }

        public override ClauseKind Kind => ClauseKind.Parameter;

        public T Value { get; }

        public string Name { get; }
    }
}
