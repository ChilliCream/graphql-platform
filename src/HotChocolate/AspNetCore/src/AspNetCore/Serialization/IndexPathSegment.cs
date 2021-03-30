namespace HotChocolate.AspNetCore.Serialization
{
    internal class IndexPathSegment : IVariablePathSegment
    {
        public IndexPathSegment(int value, IVariablePathSegment? next)
        {
            Value = value;
            Next = next;
        }

        public int Value { get; }

        public IVariablePathSegment? Next { get; }
    }
}
