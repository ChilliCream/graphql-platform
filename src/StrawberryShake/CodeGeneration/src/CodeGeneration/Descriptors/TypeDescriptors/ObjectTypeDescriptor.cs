using System.Collections.Generic;
using HotChocolate;

namespace StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

public sealed class ObjectTypeDescriptor : ComplexTypeDescriptor
{
    public ObjectTypeDescriptor(
        NameString name,
        TypeKind typeKind,
        RuntimeTypeInfo runtimeType,
        IReadOnlyList<NameString> implements,
        IReadOnlyList<DeferredFragmentDescriptor>? deferred,
        string? description,
        RuntimeTypeInfo? parentRuntimeType = null,
        IReadOnlyList<PropertyDescriptor>? properties = null)
        : base(
            name,
            typeKind,
            runtimeType,
            implements,
            deferred,
            description,
            parentRuntimeType)
    {
        if (properties is not null)
        {
            CompleteProperties(properties);
        }
    }

    public EntityTypeDescriptor EntityTypeDescriptor { get; private set; } = default!;

    public void CompleteEntityType(EntityTypeDescriptor descriptor)
    {
        EntityTypeDescriptor = descriptor;
    }
}
