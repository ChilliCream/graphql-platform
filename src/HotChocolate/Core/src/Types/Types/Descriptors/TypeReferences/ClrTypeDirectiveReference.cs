using System;

namespace HotChocolate.Types.Descriptors
{
    public sealed class ClrTypeDirectiveReference
        : IDirectiveReference
    {
        private readonly Type _clrType;

        public ClrTypeDirectiveReference(Type clrType)
        {
            _clrType = clrType ??
                throw new ArgumentNullException(nameof(clrType));
        }

        public Type ClrType 
        { 
            get 
            { 
                return _clrType; 
            }
        }
    }
}
