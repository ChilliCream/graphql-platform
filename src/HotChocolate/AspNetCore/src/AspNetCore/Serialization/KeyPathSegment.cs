namespace HotChocolate.AspNetCore.Serialization
{
    internal class KeyPathSegment : IVariablePathSegment
    {
        public KeyPathSegment(string value, IVariablePathSegment? next)
        {
            Value = value;
            Next = next;
        }

        public string Value { get; }

        public IVariablePathSegment? Next { get; }
    }
}
