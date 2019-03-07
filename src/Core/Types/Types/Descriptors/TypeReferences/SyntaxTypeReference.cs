using System;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors
{
    public class SyntaxTypeReference
        : TypeReferenceBase
        , ISyntaxTypeReference
    {
        public SyntaxTypeReference(ITypeNode type, TypeContext context)
            : this(type, context, null, null)
        {
        }

        public SyntaxTypeReference(
            ITypeNode type,
            TypeContext context,
            bool? isTypeNullable,
            bool? isElementTypeNullable)
            : base(context, isTypeNullable, isElementTypeNullable)
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
