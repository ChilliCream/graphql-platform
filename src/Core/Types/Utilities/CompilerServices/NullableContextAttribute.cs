namespace HotChocolate.Utilities.CompilerServices
{
    internal sealed class NullableContextAttribute
    {
        public readonly byte Flag;

        public NullableContextAttribute(byte flag)
        {
            Flag = flag;
        }
    }
}
