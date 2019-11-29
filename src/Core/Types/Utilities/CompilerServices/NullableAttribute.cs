namespace System.Runtime.CompilerServices
{
    [AttributeUsage(
        AttributeTargets.Class |
        AttributeTargets.GenericParameter |
        AttributeTargets.Event |
        AttributeTargets.Field |
        AttributeTargets.Property |
        AttributeTargets.Parameter |
        AttributeTargets.ReturnValue)]
    internal sealed class NullableAttribute : Attribute
    {
        private byte[] _flags;

        public NullableAttribute(byte flag)
            : this(new[] { flag })
        {
        }

        public NullableAttribute(byte[] flags)
        {
            _flags = flags;
        }

        public ReadOnlySpan<byte> Flags => _flags;
    }
}
