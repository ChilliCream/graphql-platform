using System;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class TypeSyntaxReference
        : TypeReferenceBase
        , ITypeSyntaxReference
    {
        public TypeSyntaxReference(ITypeNode type)
            : this(type, null, null)
        {
        }

        public TypeSyntaxReference(
            ITypeNode type, bool? isTypeNullable, bool? isElementTypeNullable)
            : base(isTypeNullable, isElementTypeNullable)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Type = type;
        }

        public ITypeNode Type { get; }
    }
}
