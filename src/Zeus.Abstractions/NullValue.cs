
namespace Zeus.Abstractions
{
    public sealed class NullValue
        : IValue
    {
        private NullValue() { }

        public override string ToString()
        {
            return "null";
        }

        public static NullValue Instance { get; } = new NullValue();
    }
}