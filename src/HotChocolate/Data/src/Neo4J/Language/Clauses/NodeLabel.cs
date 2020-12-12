namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Expression for a single node label.
    /// </summary>
    public class NodeLabel : Visitable
    {
        public new ClauseKind Kind { get; } = ClauseKind.NodeLabel;

        private readonly string _value;

        public NodeLabel(string value)
        {
            _value = value;
        }

        public string GetValue() => _value;

        public override string ToString() => "NodeLabel{" +
            "value='" + _value + '\'' +
            '}';
    }
}
