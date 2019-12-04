using System;

namespace HotChocolate.Utilities.CompilerServices
{
    internal sealed class NullableAttribute
    {
        private readonly byte[] _flags;

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
