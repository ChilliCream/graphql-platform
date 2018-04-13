namespace HotChocolate.Language
{
    public sealed class NullValueNode
        : IValueNode
    {
        public NullValueNode(Location location)
        {
            Location = location;
        }

        public NodeKind Kind { get; } = NodeKind.NullValue;
        public Location Location { get; }
    }
}