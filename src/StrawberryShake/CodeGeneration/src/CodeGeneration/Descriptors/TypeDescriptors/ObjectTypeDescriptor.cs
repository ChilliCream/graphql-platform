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
            IReadOnlyList<NameString> implements,
            RuntimeTypeInfo? parentRuntimeType = null,
            IReadOnlyList<PropertyDescriptor>? properties = null)
            : base(name, typeKind, runtimeType, implements, parentRuntimeType)
        {
            if (properties is not null)
            {
                CompleteProperties(properties);
            }
        }
    }
}
