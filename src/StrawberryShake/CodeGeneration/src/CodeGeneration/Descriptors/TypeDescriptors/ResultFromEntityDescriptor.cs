using System.Collections.Generic;
using HotChocolate;

namespace StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

public sealed class ResultFromEntityDescriptor : ComplexTypeDescriptor
{
    public ResultFromEntityDescriptor(
        NameString name,
        RuntimeTypeInfo runtimeType,
        IReadOnlyList<NameString> implements,
        IReadOnlyList<DeferredFragmentDescriptor>? deferred,
        string? description)
        : base(name, TypeKind.Entity, runtimeType, implements, deferred, description)
    {
    }
}
