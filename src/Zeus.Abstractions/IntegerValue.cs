
namespace Zeus.Abstractions
{
    public sealed class IntegerValue
       : ScalarValue<int>
    {
        public IntegerValue(int value)
            : base(value, NamedType.Integer)
        {
        }
    }
}