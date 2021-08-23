using System.Collections.Generic;
using HotChocolate;

namespace StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors
{
    public class ObjectTypeDescriptor : ComplexTypeDescriptor
    {
        public ObjectTypeDescriptor(
            NameString name,
            TypeKind typeKind,
            RuntimeTypeInfo runtimeType,
            IReadOnlyList<NameString> implements,
            string? description,
            RuntimeTypeInfo? parentRuntimeType = null,
            IReadOnlyList<PropertyDescriptor>? properties = null)
            : base(name, typeKind, runtimeType, implements, description, parentRuntimeType)
        {
            if (properties is not null)
            {
                CompleteProperties(properties);
            }
        }

        public EntityTypeDescriptor EntityTypeDescriptor { get; private set; }

        public void CompleteEntityType(EntityTypeDescriptor descriptor)
        {
            EntityTypeDescriptor = descriptor;
        }
    }
}
