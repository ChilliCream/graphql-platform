using System;

namespace HotChocolate.Types.Descriptors
{
    public class ClrTypeReference
        : TypeReferenceBase
        , IClrTypeReference
    {
        public ClrTypeReference(
            Type type, TypeContext context)
            : this(type, context, null, null)
        {
        }

        public ClrTypeReference(
            Type type, TypeContext context,
            bool? isTypeNullable, bool? isElementTypeNullable)
            : base(isTypeNullable, isElementTypeNullable)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Type = type;
            Context = context;
        }

        public TypeContext Context { get; }

        public Type Type { get; }
    }
}
