namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Expression for a single node label.
    /// </summary>
    public class NodeLabel : Visitable
    {
        public override ClauseKind Kind => ClauseKind.NodeLabel;

        private readonly string _value;

        public NodeLabel(string value)
        {
            _value = value;
        }

        public string GetValue() => _value;
    }
}
