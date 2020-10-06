namespace HotChocolate.Data.Neo4j
{
    public class RawClause
    {
        private readonly string _value;

        RawClause(string value)
        {
            _value = value;
        }

        public override string ToString()
        {
            return _value;
        }
    }
}
