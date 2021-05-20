namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Expression for a single node label.
    /// </summary>
    public class NodeLabel : Visitable
    {
        public override ClauseKind Kind => ClauseKind.NodeLabel;

        public NodeLabel(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }
}
