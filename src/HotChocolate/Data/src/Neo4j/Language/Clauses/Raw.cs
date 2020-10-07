namespace HotChocolate.Data.Neo4j
{
    public class Raw : IVisitable
    {
        private readonly string _value;

        Raw(string value)
        {
            _value = value;
        }

        public string Value => _value;

        public void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            visitor.Leave(this);
        }
    }
}
