namespace HotChocolate.Execution.Instrumentation
{
    internal readonly struct ListIndexPathSegment
        : IPathSegment
    {
        private readonly int _value;

        public ListIndexPathSegment(int value)
        {
            _value = value;
        }

        public static implicit operator ListIndexPathSegment(int index)
        {
            return new ListIndexPathSegment(index);
        }

        public static implicit operator int(ListIndexPathSegment segment)
        {
            return segment._value;
        }
    }
}
