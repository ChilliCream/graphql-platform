namespace HotChocolate.Data.Neo4J.Language
{
    public class Distinct : Visitable
    {
        public override ClauseKind Kind => ClauseKind.Distinct;

        private readonly bool _value;

        public Distinct(bool value)
        {
            _value = value;
        }
    }
}
