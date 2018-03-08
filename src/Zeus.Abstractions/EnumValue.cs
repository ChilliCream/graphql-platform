
namespace Zeus.Abstractions
{
    public sealed class EnumValue
       : ScalarValue<string>
    {
        public EnumValue(string value)
            : base(value)
        {
        }
    }
}