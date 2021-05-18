namespace HotChocolate.Data.Neo4J.Language
{
    public class Parameter<T> : Expression
    {
        private readonly string _name;

        public Parameter(string name, T value)
        {
            _name = name;
            Value = value;
        }

        public override ClauseKind Kind => ClauseKind.Parameter;

        public T Value { get; }
    }
}
