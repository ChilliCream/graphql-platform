using System;

namespace HotChocolate.Types.Descriptors
{
    public class SchemaTypeReference
        : TypeReferenceBase
        , ISchemaTypeReference
    {
        public SchemaTypeReference(IType type)
            : this(type, null, null)
        {
        }

        public SchemaTypeReference(
            IType type, bool? isTypeNullable, bool? isElementTypeNullable)
            : base(TypeContext.None, isTypeNullable, isElementTypeNullable)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Type = type;
        }

        public IType Type { get; }
    }
}
