namespace HotChocolate.Data.Neo4J.Language
{
    public class Distinct : Visitable
    {
        public new ClauseKind Kind { get; } = ClauseKind.Distinct;

        private readonly bool _value;

        public Distinct(bool value)
        {
            _value = value;
        }

        public bool GetValue() => _value;
    }
}