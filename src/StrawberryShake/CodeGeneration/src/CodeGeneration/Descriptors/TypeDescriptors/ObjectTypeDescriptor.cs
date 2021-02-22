using System.Collections.Generic;
using HotChocolate;

namespace StrawberryShake.CodeGeneration
{
    public class ObjectTypeDescriptor : ComplexTypeDescriptor
    {
        public ObjectTypeDescriptor(
            NameString name,
            TypeKind typeKind,
            RuntimeTypeInfo runtimeType,
            IReadOnlyList<ComplexTypeDescriptor> implementedBy,
            IReadOnlyList<NameString> implements,
            RuntimeTypeInfo? parentRuntimeType = null)
            : base(name, typeKind, runtimeType, implementedBy, implements, parentRuntimeType)
        {
        }
    }
}
