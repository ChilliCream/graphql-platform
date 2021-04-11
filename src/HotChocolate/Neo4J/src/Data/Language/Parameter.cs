namespace HotChocolate.Data.Neo4J.Language
{
    public class Parameter<T> : Expression
    {
        public override ClauseKind Kind => ClauseKind.Parameter;
        private readonly string _name;
        private readonly T _value;

        public Parameter(string name, T value)
        {
            _name = name;
            _value = value;
        }

        public T GetValue() => _value;
    }
}
