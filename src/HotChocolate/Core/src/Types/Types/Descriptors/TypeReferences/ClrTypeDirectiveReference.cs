using System;

#nullable enable

namespace HotChocolate.Types.Descriptors
{
    public sealed class ClrTypeDirectiveReference : IDirectiveReference
    {
        public ClrTypeDirectiveReference(Type clrType)
        {
            ClrType = clrType ?? throw new ArgumentNullException(nameof(clrType));
        }

        public Type ClrType { get; }
    }
}
