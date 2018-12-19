using System.Collections.Generic;

namespace HotChocolate.Types
{
    public class TypeBase
        : TypeSystemBase
        , IType
    {
        public TypeBase(TypeKind kind)
        {
            Kind = kind;
        }

        public TypeKind Kind { get; }
    }
}
