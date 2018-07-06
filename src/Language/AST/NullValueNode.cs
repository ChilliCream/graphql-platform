namespace HotChocolate.Language
{
    public sealed class NullValueNode
        : IValueNode<object>
    {
        public NullValueNode()
        {
        }

        public NullValueNode(Location location)
        {
            Location = location;
        }

        public NodeKind Kind { get; } = NodeKind.NullValue;

        public Location Location { get; }

        public object Value { get; } = null;
    }
}
