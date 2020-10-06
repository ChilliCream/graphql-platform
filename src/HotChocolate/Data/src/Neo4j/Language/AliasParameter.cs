namespace HotChocolate.Data.Neo4j
{
    public class AliasParameter
    {
        private readonly string _value;

        public AliasParameter(string value)
        {
            _value = value;
        }

        public string getValue()
        {
            return _value;
        }
    }
}
