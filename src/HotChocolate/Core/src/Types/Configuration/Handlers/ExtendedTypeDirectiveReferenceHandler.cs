#nullable enable
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration;

internal sealed class ExtendedTypeDirectiveReferenceHandler(ITypeInspector typeInspector)
    : ITypeRegistrarHandler
{
    public TypeReferenceKind Kind => TypeReferenceKind.DirectiveExtendedType;

    public void Handle(ITypeRegistrar typeRegistrar, TypeReference typeReference)
    {
        var typeRef = (ExtendedTypeDirectiveReference)typeReference;

        if (typeInspector.TryCreateTypeInfo(typeRef.Type, out var typeInfo) &&
            !ExtendedType.Tools.IsSchemaType(typeInfo.NamedType) &&
            !typeRegistrar.IsResolved(typeRef))
        {
            typeRegistrar.MarkUnresolved(typeRef);
        }
    }
}
