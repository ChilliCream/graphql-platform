namespace HotChocolate.Data.Neo4J.Language
{
    public class ConstantParameterHolder
    {
        private readonly object _value;
        private readonly Expression _literalValue;

        public ConstantParameterHolder(object value)
        {
            _value = value;
            _literalValue = Cypher.LiteralOf(value);
        }

        public object GetValue() => _value;

        public string AsString() => _literalValue.ToString();
    }
}
