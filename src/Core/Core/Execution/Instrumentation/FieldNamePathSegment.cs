namespace HotChocolate.Execution.Instrumentation
{
    internal readonly struct FieldNamePathSegment
        : IPathSegment
    {
        private readonly string _value;

        public FieldNamePathSegment(string value)
        {
            _value = value;
        }

        public static implicit operator FieldNamePathSegment(string name)
        {
            return new FieldNamePathSegment(name);
        }

        public static implicit operator string(FieldNamePathSegment segment)
        {
            return segment._value;
        }
    }
}
