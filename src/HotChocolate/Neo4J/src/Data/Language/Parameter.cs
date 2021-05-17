namespace HotChocolate.Data.Neo4J.Language
{
    public class Parameter<T> : Expression
    {
        private readonly string _name;
        private readonly T _value;

        public Parameter(string name, T value)
        {
            _name = name;
            _value = value;
        }

        public override ClauseKind Kind => ClauseKind.Parameter;

        public T GetValue() => _value;
    }
}
